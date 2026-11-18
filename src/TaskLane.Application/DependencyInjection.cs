using FluentValidation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using TaskLane.Application.Identity.Register;

namespace TaskLane.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers use-case handlers and validators. Each feature slice adds its own registrations here.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IValidator<RegisterUserCommand>, RegisterUserValidator>();
        services.AddScoped<RegisterUserHandler>();

        return services;
    }
}
