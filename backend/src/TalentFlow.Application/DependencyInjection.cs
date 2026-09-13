using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace TalentFlow.Application;

/// <summary>
/// Registers Application layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
