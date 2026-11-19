using System.Text.Json.Serialization;

using Scalar.AspNetCore;

using TaskLane.Api.Identity;
using TaskLane.Application;
using TaskLane.Infrastructure;
using TaskLane.Infrastructure.Identity;
using TaskLane.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddTokenAuthentication();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // Enums travel by name ("ToDo"), never by number; DateOnly is already "yyyy-MM-dd".
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
    await app.Services.SeedDemoAccountAsync();

    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();
