using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace EcommerceApp.Services;

public sealed class SprintFeatureOptions
{
    public int ActiveSprint { get; set; } = 1;
}

public interface ISprintFeatureService
{
    int ActiveSprint { get; }
    bool IsEnabled(int sprint);
}

public sealed class SprintFeatureService : ISprintFeatureService
{
    public const int LatestSprint = 3;

    public static int Normalize(int sprint) => Math.Clamp(sprint, 1, LatestSprint);

    public SprintFeatureService(IOptions<SprintFeatureOptions> options)
    {
        ActiveSprint = Normalize(options.Value.ActiveSprint);
    }

    public int ActiveSprint { get; }

    public bool IsEnabled(int sprint) => ActiveSprint >= sprint;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class MinimumSprintAttribute : Attribute, IResourceFilter, IOrderedFilter
{
    public MinimumSprintAttribute(int sprint)
    {
        Sprint = sprint;
    }

    public int Sprint { get; }

    // Use the earliest resource-filter order; startup middleware also blocks before authorization.
    public int Order => int.MinValue;

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var features = context.HttpContext.RequestServices.GetRequiredService<ISprintFeatureService>();
        if (!features.IsEnabled(Sprint))
        {
            context.Result = new NotFoundResult();
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
