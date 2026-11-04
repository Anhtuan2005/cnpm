using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EcommerceApp.Data;
using EcommerceApp.Models;
using EcommerceApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApp.Services;

public class GeminiChatService : IAiChatService
{
    private const int MaxHistoryItems = 8;
    private const int MaxProductsInPrompt = 18;
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly ICatalogScope? _catalogScope;

    public GeminiChatService(
        HttpClient httpClient,
        AppDbContext db,
        IConfiguration configuration,
        ILogger<GeminiChatService> logger,
        ICatalogScope? catalogScope = null)
    {
        _httpClient = httpClient;
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _catalogScope = catalogScope;
    }

    public async Task<AiChatResponse> AskAsync(string message, IReadOnlyList<AiChatHistoryItem> history, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return await BuildCatalogReplyAsync(message, cancellationToken);
        }

        var model = _configuration["Gemini:Model"] ?? "gemini-3.5-flash";
        var baseUrl = (_configuration["Gemini:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta").TrimEnd('/');
        var endpoint = $"{baseUrl}/{NormalizeModelPath(model)}:generateContent";
        var catalogContext = await BuildCatalogContextAsync(message, cancellationToken);

        var request = new GeminiGenerateRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = new List<GeminiPart>
                {
                    new() { Text = BuildSystemInstruction(catalogContext) }
                }
            },
            Contents = BuildConversation(message, history),
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = 0.55,
                TopP = 0.9,
                MaxOutputTokens = 2200,
                ThinkingConfig = BuildThinkingConfig(model)
            }
        };

        try
        {
            using var response = await SendGeminiRequestWithRetryAsync(endpoint, apiKey, request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API returned {StatusCode}.", response.StatusCode);
                return await BuildCatalogReplyAsync(message, cancellationToken);
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiGenerateResponse>(JsonOptions, cancellationToken);
            var answer = ExtractText(result);
            if (string.IsNullOrWhiteSpace(answer))
            {
                return await BuildCatalogReplyAsync(message, cancellationToken);
            }

            return new AiChatResponse
            {
                Reply = answer.Trim(),
                Products = await BuildProductLinksAsync(answer, cancellationToken)
            };
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await BuildCatalogReplyAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Gemini request failed with {ErrorType}.", ex.GetType().Name);
            return await BuildCatalogReplyAsync(message, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Gemini response could not be parsed: {ErrorType}.", ex.GetType().Name);
            return await BuildCatalogReplyAsync(message, cancellationToken);
        }
    }

    private async Task<List<AiChatProductLink>> BuildProductLinksAsync(string answer, CancellationToken cancellationToken)
    {
        var ids = ExtractMentionedProductIds(answer).Take(6).ToList();
        var query = _db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.Stock > 0)
            .AsQueryable();
        if (_catalogScope is not null)
            query = _catalogScope.Products(query);

        var products = ids.Count > 0
            ? await query.Where(product => ids.Contains(product.Id)).ToListAsync(cancellationToken)
            : await query
                .OrderByDescending(product => product.IsFeatured)
                .ThenByDescending(product => product.DiscountPercent)
                .Take(30)
                .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            products = products
                .Where(product => answer.Contains(product.Name, StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList();
        }

        return products
            .OrderBy(product => ids.IndexOf(product.Id) < 0 ? int.MaxValue : ids.IndexOf(product.Id))
            .Take(4)
            .Select(product => new AiChatProductLink
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category?.Name ?? "Sản phẩm",
                Price = FormatMoney(product.SalePrice),
                Url = $"/Product/Detail/{product.Id}"
            })
            .ToList();
    }

    private static AiChatResponse TextOnly(string reply)
    {
        return new AiChatResponse { Reply = reply };
    }

    private async Task<AiChatResponse> BuildCatalogReplyAsync(string message, CancellationToken cancellationToken)
    {
        var products = (await FindCatalogProductsAsync(message, cancellationToken)).Take(4).ToList();
        if (products.Count == 0)
            return TextOnly("Chưa có sản phẩm còn hàng phù hợp với hãng và ngân sách bạn nêu. Bạn có thể thử hãng khác hoặc điều chỉnh ngân sách.");

        return new AiChatResponse
        {
            Reply = "Mình gợi ý từ danh mục hiện có:\n"
                + string.Join("\n", products.Select(product => $"• {product.Name}: {FormatMoney(product.SalePrice)}."))
                + "\nBạn có thể mở chi tiết để xem thông số, hoặc cho mình biết hãng và mức giá mong muốn.",
            Products = products.Select(product => new AiChatProductLink
            {
                Id = product.Id, Name = product.Name, Category = product.Category?.Name ?? "Điện thoại",
                Price = FormatMoney(product.SalePrice), Url = $"/Product/Detail/{product.Id}"
            }).ToList()
        };
    }

    private async Task<HttpResponseMessage> SendGeminiRequestWithRetryAsync(string endpoint, string apiKey, GeminiGenerateRequest request, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            message.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);

            var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!ShouldRetry(response.StatusCode) || attempt == maxAttempts)
            {
                return response;
            }

            _logger.LogWarning("Gemini API returned {StatusCode}. Retrying attempt {NextAttempt}/{MaxAttempts}.", response.StatusCode, attempt + 1, maxAttempts);
            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt * attempt), cancellationToken);
        }

        throw new InvalidOperationException("Gemini retry loop ended unexpectedly.");
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static List<int> ExtractMentionedProductIds(string answer)
    {
        return Regex.Matches(answer, @"(?:/Product/Detail/|#)(\d+)", RegexOptions.IgnoreCase)
            .Select(match => int.TryParse(match.Groups[1].Value, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    private static GeminiThinkingConfig? BuildThinkingConfig(string model)
    {
        var normalized = model.Trim().ToLowerInvariant();

        if (normalized.StartsWith("gemini-3", StringComparison.Ordinal))
        {
            return new GeminiThinkingConfig { ThinkingLevel = "minimal" };
        }

        if (normalized.StartsWith("gemini-2.5", StringComparison.Ordinal))
        {
            return new GeminiThinkingConfig { ThinkingBudget = 0 };
        }

        return null;
    }

    private async Task<List<Product>> FindCatalogProductsAsync(string message, CancellationToken cancellationToken)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.Stock > 0)
            .AsQueryable();
        if (_catalogScope is not null)
            query = _catalogScope.Products(query);
        var normalized = NormalizeSearchText(message);
        var asksForAccessories = new[] { "phu kien", "op lung", "tai nghe", "cu sac", "cap sac", "cap usb", "kinh cuong luc", "earphone", "charger", "phone case" }
            .Any(term => normalized.Contains(term, StringComparison.Ordinal));
        query = asksForAccessories ? PhoneCatalogScope.OnlyAccessories(query) : PhoneCatalogScope.OnlyPhones(query);
        if (asksForAccessories)
        {
            var phones = await PhoneCatalogScope.OnlyPhones(_db.Products).Select(product => new { product.Id, product.Name }).ToListAsync(cancellationToken);
            var phone = phones.OrderByDescending(product => product.Name.Length)
                .FirstOrDefault(product => normalized.Contains(NormalizeSearchText(product.Name), StringComparison.Ordinal));
            var isCase = normalized.Contains("op lung", StringComparison.Ordinal) || normalized.Contains("phone case", StringComparison.Ordinal);
            var isGlass = normalized.Contains("kinh cuong luc", StringComparison.Ordinal);
            if ((isCase || isGlass) && phone is null) return new List<Product>();
            if (phone is not null)
            {
                var now = DateTime.UtcNow;
                query = query.Where(product => _db.CrossSellOffers.Any(offer => offer.AnchorProductId == phone.Id
                    && offer.AddOnProductId == product.Id && offer.IsActive
                    && (offer.StartDate == null || offer.StartDate <= now) && (offer.EndDate == null || offer.EndDate >= now)));
            }
            if (isCase) query = query.Where(product => product.Name.Contains("Ốp lưng"));
            else if (isGlass) query = query.Where(product => product.Name.Contains("Kính"));
            else if (normalized.Contains("tai nghe") || normalized.Contains("earphone")) query = query.Where(product => product.Name.Contains("Tai nghe"));
            else if (normalized.Contains("cu sac") || normalized.Contains("charger")) query = query.Where(product => product.Name.Contains("Củ sạc"));
            else if (normalized.Contains("cap sac") || normalized.Contains("cap usb")) query = query.Where(product => product.Name.Contains("Cáp"));
        }
        var brand = new[] { "iphone", "samsung", "xiaomi", "oppo", "vivo", "realme", "pixel", "sony" }
            .FirstOrDefault(name => normalized.Contains(name, StringComparison.Ordinal));
        if (brand is not null && !asksForAccessories)
            query = query.Where(product => product.Name.Contains(brand));
        var budgetMatch = Regex.Match(message, @"(\d+(?:[.,]\d+)?)\s*(?:triệu|trieu|tr\b)", RegexOptions.IgnoreCase);
        if (budgetMatch.Success && decimal.TryParse(budgetMatch.Groups[1].Value.Replace(',', '.'),
                NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var millions) && millions > 0 && millions <= 1000)
        {
            var budget = millions * 1_000_000m;
            query = query.Where(product => product.Price * (100 - product.DiscountPercent) / 100m <= budget);
        }
        var products = await query.ToListAsync(cancellationToken);
        var terms = ExtractSearchTerms(message);
        return products
            .Select(product => new
            {
                Product = product,
                Score = ScoreProduct(product, terms)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Product.IsFeatured)
            .ThenByDescending(item => item.Product.DiscountPercent)
            .ThenBy(item => item.Product.SalePrice)
            .Take(MaxProductsInPrompt)
            .Select(item => item.Product)
            .ToList();
    }

    private async Task<string> BuildCatalogContextAsync(string message, CancellationToken cancellationToken)
    {
        var rankedProducts = await FindCatalogProductsAsync(message, cancellationToken);
        if (rankedProducts.Count == 0)
            return "Hiện chưa có điện thoại còn hàng phù hợp hãng và ngân sách đã nêu. Hãy hỏi khách điều chỉnh tiêu chí.";

        var builder = new StringBuilder();
        builder.AppendLine("Catalog sản phẩm còn hàng của Techvora:");
        foreach (var product in rankedProducts)
        {
            var discount = product.DiscountPercent > 0 ? $", giảm {product.DiscountPercent}%" : string.Empty;
            builder
                .Append("- #")
                .Append(product.Id)
                .Append(": ")
                .Append(product.Name)
                .Append(" | Danh mục: ")
                .Append(product.Category?.Name ?? "Chưa phân loại")
                .Append(" | Giá bán: ")
                .Append(FormatMoney(product.SalePrice))
                .Append(discount)
                .Append(" | Tồn: ")
                .Append(product.Stock)
                .Append(" | Link: /Product/Detail/")
                .Append(product.Id);

            var description = CompactText(product.Description, 180);
            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.Append(" | Mô tả: ").Append(description);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildSystemInstruction(string catalogContext)
    {
        return $"""
Bạn là trợ lý tư vấn mua sắm của Techvora, trả lời bằng tiếng Việt tự nhiên, ngắn gọn và hữu ích.
Nhiệm vụ: hỏi thêm khi thiếu nhu cầu, tư vấn điện thoại và phụ kiện điện thoại theo ngân sách.
Chỉ khẳng định phụ kiện tương thích với máy khi mô tả catalog nêu rõ; không suy đoán ốp lưng, kính hoặc cổng cáp của mẫu máy khác.
Chỉ dùng dữ liệu sản phẩm được cung cấp bên dưới khi nói về tên sản phẩm, giá, tồn kho hoặc link. Không bịa sản phẩm, giá hay khuyến mãi.
Khi gợi ý sản phẩm, chọn tối đa 4 lựa chọn. Mỗi lựa chọn phải ghi đủ: tên sản phẩm, mã #id, giá bán, lý do ngắn và link /Product/Detail/id.
Ưu tiên trả lời trong 4 đến 7 dòng. Hoàn thành câu trả lời, không dừng giữa câu.
Nếu câu hỏi không liên quan mua sắm công nghệ, lịch sự kéo cuộc trò chuyện về tư vấn sản phẩm Techvora.
Không cam kết giá tuyệt đối; nhắc khách kiểm tra trang chi tiết trước khi mua nếu cần.

{catalogContext}
""";
    }

    private static List<GeminiContent> BuildConversation(string message, IReadOnlyList<AiChatHistoryItem> history)
    {
        var contents = new List<GeminiContent>();
        foreach (var item in history.TakeLast(MaxHistoryItems))
        {
            if (item is null) continue;
            var text = CompactText(item.Text, 1200);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            contents.Add(new GeminiContent
            {
                Role = string.Equals(item.Role, "model", StringComparison.OrdinalIgnoreCase) ? "model" : "user",
                Parts = new List<GeminiPart> { new() { Text = text } }
            });
        }

        contents.Add(new GeminiContent
        {
            Role = "user",
            Parts = new List<GeminiPart> { new() { Text = CompactText(message, 1600) } }
        });

        return contents;
    }

    private static string ExtractText(GeminiGenerateResponse? response)
    {
        if (response?.Candidates is null)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            response.Candidates
                .SelectMany(candidate => candidate.Content?.Parts ?? new List<GeminiPart>())
                .Select(part => part.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static int ScoreProduct(Product product, string[] terms)
    {
        var haystack = NormalizeSearchText($"{product.Name} {product.Category?.Name} {product.Description}");
        var score = product.IsFeatured ? 6 : 0;
        score += product.DiscountPercent > 0 ? 4 : 0;

        foreach (var term in terms)
        {
            if (haystack.Contains(term, StringComparison.Ordinal))
            {
                score += 10;
            }
        }

        return score;
    }

    private static string[] ExtractSearchTerms(string message)
    {
        return NormalizeSearchText(message)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 2)
            .Distinct()
            .Take(12)
            .ToArray();
    }

    private static string NormalizeSearchText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CompactText(string value, int maxLength)
    {
        var compact = string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= maxLength ? compact : compact[..maxLength].TrimEnd() + "...";
    }

    private static string FormatMoney(decimal value)
    {
        return value.ToString("N0", VietnameseCulture) + " đ";
    }

    private static string NormalizeModelPath(string model)
    {
        var trimmed = model.Trim().Trim('/');
        return trimmed.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? trimmed : $"models/{trimmed}";
    }

    private sealed class GeminiGenerateRequest
    {
        [JsonPropertyName("contents")]
        public List<GeminiContent> Contents { get; set; } = new();

        [JsonPropertyName("systemInstruction")]
        public GeminiContent? SystemInstruction { get; set; }

        [JsonPropertyName("generationConfig")]
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class GeminiGenerationConfig
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("topP")]
        public double TopP { get; set; }

        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonPropertyName("thinkingConfig")]
        public GeminiThinkingConfig? ThinkingConfig { get; set; }
    }

    private sealed class GeminiThinkingConfig
    {
        [JsonPropertyName("thinkingBudget")]
        public int? ThinkingBudget { get; set; }

        [JsonPropertyName("thinkingLevel")]
        public string? ThinkingLevel { get; set; }
    }

    private sealed class GeminiGenerateResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
