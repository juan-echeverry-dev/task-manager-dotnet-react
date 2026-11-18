using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskLane.Api.Tests.Support;

/// <summary>
/// Test-only endpoints, added to the API under test through <see cref="ApiFactory"/>.
/// </summary>
[ApiController]
[Route("test-probes")]
public sealed class ProbeController : ControllerBase
{
    /// <summary>Declares no authorization attributes, so the fallback policy applies.</summary>
    [HttpGet("protected")]
    public IActionResult Protected() => Ok();

    [AllowAnonymous]
    [HttpGet("throws")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "MVC actions must be instance methods.")]
    public IActionResult Throws() =>
        throw new InvalidOperationException("Sensitive internal detail: Data Source=secret.db");
}
