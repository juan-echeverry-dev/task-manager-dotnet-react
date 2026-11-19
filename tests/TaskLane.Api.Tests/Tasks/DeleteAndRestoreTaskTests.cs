using System.Net;
using System.Text.Json;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

public sealed class DeleteAndRestoreTaskTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Delete_Returns204AndIsIdempotent()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);

        (await DeleteAsync(id, token)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await DeleteAsync(id, token)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletedTask_LeavesTheListAndAppearsAmongDeletedTasks()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var keep = await CreateAsync(token, "Keep");
        var first = await CreateAsync(token, "Deleted first");
        var last = await CreateAsync(token, "Deleted last");
        await DeleteAsync(first, token);
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(1));
        await DeleteAsync(last, token);

        var active = await (await _client.GetAsync("/api/tasks", token)).ReadJsonAsync();
        var deletedResponse = await _client.GetAsync("/api/tasks/deleted", token);

        active.Titles().ShouldBe(["Keep"]);
        active.GetProperty("totalCount").GetInt32().ShouldBe(1);
        deletedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var deleted = await deletedResponse.ReadJsonAsync();
        deleted.Titles().ShouldBe(["Deleted last", "Deleted first"]);
        deleted.GetProperty("items")[0].GetProperty("deletedAt").GetDateTimeOffset()
            .ShouldBe(factory.TimeProvider.GetUtcNow());
        keep.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task DeletedTask_CanStillBeViewed()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        await DeleteAsync(id, token);

        var response = await _client.GetAsync($"/api/tasks/{id}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadJsonAsync()).GetProperty("deletedAt").ValueKind.ShouldBe(JsonValueKind.String);
    }

    [Fact]
    public async Task EditingADeletedTask_Returns409AndChangesNothing()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        await DeleteAsync(id, token);

        var response = await _client.SendAsync(HttpMethod.Put, $"/api/tasks/{id}", token, new { title = "Changed", status = "Done" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var problem = await response.ReadJsonAsync();
        problem.GetProperty("title").GetString().ShouldBe("Task is deleted");
        problem.GetProperty("detail").GetString().ShouldBe("Restore the task before editing it.");
        var task = await (await _client.GetAsync($"/api/tasks/{id}", token)).ReadJsonAsync();
        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
        task.GetProperty("status").GetString().ShouldBe("InProgress");
    }

    [Fact]
    public async Task Restore_ReturnsTheTaskWithEveryValueAsBeforeDeletion()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        var before = await (await _client.GetAsync($"/api/tasks/{id}", token)).ReadJsonAsync();
        await DeleteAsync(id, token);
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(5));

        var response = await RestoreAsync(id, token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var restored = await response.ReadJsonAsync();
        restored.GetRawText().ShouldBe(before.GetRawText());
        (await (await _client.GetAsync("/api/tasks", token)).ReadJsonAsync()).Titles().ShouldBe(["Prepare demo"]);
        (await RestoreAsync(id, token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnotherAccountsTask_CannotBeDeletedOrRestored()
    {
        var (_, owner) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(owner);
        var (_, intruder) = await _client.RegisterAndSignInAsync();
        var missing = Guid.CreateVersion7();

        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(await DeleteAsync(id, intruder), await DeleteAsync(missing, intruder));
        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(await RestoreAsync(id, intruder), await RestoreAsync(missing, intruder));

        var task = await (await _client.GetAsync($"/api/tasks/{id}", owner)).ReadJsonAsync();
        task.GetProperty("deletedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        (await (await _client.GetAsync("/api/tasks/deleted", intruder)).ReadJsonAsync()).Titles().ShouldBeEmpty();
    }

    [Fact]
    public async Task ActiveList_RejectsSortingByDeletionDate()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync("/api/tasks?sort=deletedAt", token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty("sort")[0].GetString()
            .ShouldBe("Sort must be one of: dueDate, createdAt, title.");
    }

    private async Task<Guid> CreateAsync(string token, string title = "Prepare demo") =>
        (await _client.CreateTaskAndReadAsync(token, new { title, description = "Slides", status = "InProgress", dueDate = "2026-12-01" }))
            .GetProperty("id").GetGuid();

    private Task<HttpResponseMessage> DeleteAsync(Guid id, string token) =>
        _client.SendAsync(HttpMethod.Delete, $"/api/tasks/{id}", token);

    private Task<HttpResponseMessage> RestoreAsync(Guid id, string token) =>
        _client.SendAsync(HttpMethod.Post, $"/api/tasks/{id}/restore", token);
}
