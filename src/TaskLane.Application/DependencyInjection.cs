using FluentValidation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using TaskLane.Application.Identity.GetProfile;
using TaskLane.Application.Identity.Register;
using TaskLane.Application.Identity.SignIn;
using TaskLane.Application.Tasks.CreateTask;
using TaskLane.Application.Tasks.ListTasks;

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

        services.AddSingleton<IValidator<SignInCommand>, SignInValidator>();
        services.AddScoped<SignInHandler>();

        services.AddScoped<GetProfileHandler>();

        services.AddSingleton<IValidator<CreateTaskCommand>, CreateTaskValidator>();
        services.AddScoped<CreateTaskHandler>();

        services.AddSingleton<IValidator<ListTasksQuery>, ListTasksValidator>();
        services.AddScoped<ListTasksHandler>();

        return services;
    }
}
