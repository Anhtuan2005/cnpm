using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EcommerceApp.Models.ViewModels;

namespace EcommerceApp.Tests;

public sealed class RegistrationValidationDataTests
{
    public static IEnumerable<object[]> Cases()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "registration_test_data.json");
        var data = JsonSerializer.Deserialize<List<RegistrationCase>>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return (data ?? throw new InvalidDataException("Registration test data is empty."))
            .Select(item => new object[] { item });
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Registration_model_matches_test_data(RegistrationCase item)
    {
        var model = new RegisterViewModel
        {
            FullName = item.FullName,
            Email = item.Email,
            PhoneNumber = item.PhoneNumber,
            Address = item.Address,
            Password = item.Password,
            ConfirmPassword = item.ConfirmPassword
        };

        var errors = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(model, new ValidationContext(model), errors, validateAllProperties: true);

        Assert.Equal(item.ExpectedValid, valid);
        if (!item.ExpectedValid)
            Assert.Contains(errors, error => error.MemberNames.Contains(item.ExpectedField));
    }

    public sealed record RegistrationCase(
        string Id,
        string FullName,
        string Email,
        string PhoneNumber,
        string? Address,
        string Password,
        string ConfirmPassword,
        bool ExpectedValid,
        string? ExpectedField)
    {
        public override string ToString() => Id;
    }
}
