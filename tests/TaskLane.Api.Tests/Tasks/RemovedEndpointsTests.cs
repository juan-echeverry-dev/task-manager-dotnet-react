using System.Net;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

/// <summary>Nothing of the former deleted-tasks area is left in the API (spec 003 US2, FR-005).</summary>
public sealed class RemovedEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task DeletedTasksList_IsNotAvailable()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        (await _client.GetAsync("/api/tasks/deleted", token)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Restore_IsNotAvailable()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);

        (await _client.SendAsync(HttpMethod.Post, $"/api/tasks/{id}/restore", token)).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_RejectsSortingByDeletionDate()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync("/api/tasks?sort=deletedAt", token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty("sort")[0].GetString()
            .ShouldBe("Sort must be one of: dueDate, createdAt, title.");
    }

    [Fact]
    public async Task Task_HasNoDeletionDate()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);

        var task = await (await _client.GetAsync($"/api/tasks/{id}", token)).ReadJsonAsync();

        task.TryGetProperty("deletedAt", out _).ShouldBeFalse();
    }

    private async Task<Guid> CreateAsync(string token) =>
        (await _client.CreateTaskAndReadAsync(token, new { title = "Prepare demo" })).GetProperty("id").GetGuid();
}
