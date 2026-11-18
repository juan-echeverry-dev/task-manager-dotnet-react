using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        return services;
    }
}
