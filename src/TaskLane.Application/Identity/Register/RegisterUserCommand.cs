namespace TaskLane.Application.Identity.Register;

public sealed record RegisterUserCommand(string DisplayName, string Email, string Password);
