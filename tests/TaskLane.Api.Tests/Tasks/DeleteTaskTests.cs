using System.Net;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

/// <summary>Permanent deletion (spec 003 US1, contracts/tasks-api.md).</summary>
public sealed class DeleteTaskTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Delete_Returns204AndRemovesTheTaskFromTheListAndCount()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        await CreateAsync(token, "Keep");
        var id = await CreateAsync(token, "Remove");

        (await DeleteAsync(id, token)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var page = await (await _client.GetAsync("/api/tasks", token)).ReadJsonAsync();
        page.Titles().ShouldBe(["Keep"]);
        page.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task DeletedTask_CannotBeOpenedEditedOrDeletedAgain()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        await DeleteAsync(id, token);
        var missing = Guid.CreateVersion7();

        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(
            await _client.GetAsync($"/api/tasks/{id}", token), await _client.GetAsync($"/api/tasks/{missing}", token));
        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(
            await UpdateAsync(id, token), await UpdateAsync(missing, token));
        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(await DeleteAsync(id, token), await DeleteAsync(missing, token));
    }

    [Fact]
    public async Task AnotherAccountsTask_CannotBeDeleted()
    {
        var (_, owner) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(owner);
        var (_, intruder) = await _client.RegisterAndSignInAsync();

        await GetAndUpdateTaskTests.ShouldBeSameNotFoundAsync(
            await DeleteAsync(id, intruder), await DeleteAsync(Guid.CreateVersion7(), intruder));

        (await _client.GetAsync($"/api/tasks/{id}", owner)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_WithoutASession_Returns401()
    {
        (await DeleteAsync(Guid.CreateVersion7(), null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CreateAsync(string token, string title = "Prepare demo") =>
        (await _client.CreateTaskAndReadAsync(token, new { title })).GetProperty("id").GetGuid();

    private Task<HttpResponseMessage> DeleteAsync(Guid id, string? token) =>
        _client.SendAsync(HttpMethod.Delete, $"/api/tasks/{id}", token);

    private Task<HttpResponseMessage> UpdateAsync(Guid id, string token) =>
        _client.SendAsync(HttpMethod.Put, $"/api/tasks/{id}", token, new { title = "Changed", status = "Done" });
}
