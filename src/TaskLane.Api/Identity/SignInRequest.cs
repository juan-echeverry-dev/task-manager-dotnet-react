namespace TaskLane.Api.Identity;

/// <summary>Fields are nullable so that missing values reach the Application validator as field errors.</summary>
public sealed record SignInRequest(string? Email, string? Password);
