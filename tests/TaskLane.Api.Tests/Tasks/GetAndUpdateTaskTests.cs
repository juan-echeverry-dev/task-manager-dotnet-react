using System.Net;
using System.Text.Json;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

public sealed class GetAndUpdateTaskTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_OwnTask_Returns200WithAllValues()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var created = await _client.CreateTaskAndReadAsync(token, new
        {
            title = "Prepare demo",
            description = "Slides",
            status = "InProgress",
            dueDate = "2026-12-01",
        });
        var id = created.GetProperty("id").GetGuid();

        var response = await _client.GetAsync($"/api/tasks/{id}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var task = await response.ReadJsonAsync();
        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
        task.GetProperty("description").GetString().ShouldBe("Slides");
        task.GetProperty("status").GetString().ShouldBe("InProgress");
        task.GetProperty("dueDate").GetString().ShouldBe("2026-12-01");
        task.GetProperty("createdAt").GetDateTimeOffset().ShouldBe(created.GetProperty("createdAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Update_ReplacesValuesAndTheDetailShowsThem()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(30));

        var response = await UpdateAsync(id, token, new
        {
            title = "Prepare the demo",
            description = "Slides and seed data",
            status = "Done",
            dueDate = "2026-12-02",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.ReadJsonAsync();
        updated.GetProperty("updatedAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow());
        var task = await (await _client.GetAsync($"/api/tasks/{id}", token)).ReadJsonAsync();
        task.GetProperty("title").GetString().ShouldBe("Prepare the demo");
        task.GetProperty("description").GetString().ShouldBe("Slides and seed data");
        task.GetProperty("status").GetString().ShouldBe("Done");
        task.GetProperty("dueDate").GetString().ShouldBe("2026-12-02");
    }

    [Fact]
    public async Task Update_WithNullDescriptionAndDueDate_ClearsThem()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);

        var task = await (await UpdateAsync(id, token, new
        {
            title = "Prepare demo",
            description = (string?)null,
            status = "ToDo",
            dueDate = (string?)null,
        })).ReadJsonAsync();

        task.GetProperty("description").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("dueDate").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Update_FromDoneBackToToDo_ReopensTheTask()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);
        await UpdateAsync(id, token, new { title = "Prepare demo", status = "Done" });

        var task = await (await UpdateAsync(id, token, new { title = "Prepare demo", status = "ToDo" })).ReadJsonAsync();

        task.GetProperty("status").GetString().ShouldBe("ToDo");
    }

    [Fact]
    public async Task Update_WithInvalidValues_Returns400AndChangesNothing()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(token);

        var response = await UpdateAsync(id, token, new { title = "", status = (string?)null, dueDate = "2100-01-01" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        errors.GetProperty("title")[0].GetString().ShouldBe("Title is required.");
        errors.GetProperty("status")[0].GetString().ShouldBe("Status is required.");
        errors.GetProperty("dueDate")[0].GetString().ShouldBe("Enter a due date between 2000-01-01 and 2099-12-31.");
        var task = await (await _client.GetAsync($"/api/tasks/{id}", token)).ReadJsonAsync();
        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
    }

    [Fact]
    public async Task AnotherAccountsTask_IsIndistinguishableFromAMissingOne()
    {
        var (_, owner) = await _client.RegisterAndSignInAsync();
        var id = await CreateAsync(owner);
        var (_, intruder) = await _client.RegisterAndSignInAsync();
        var missing = Guid.CreateVersion7();
        var update = new { title = "Hijacked", status = "Done" };

        await ShouldBeSameNotFoundAsync(
            await _client.GetAsync($"/api/tasks/{id}", intruder),
            await _client.GetAsync($"/api/tasks/{missing}", intruder));
        await ShouldBeSameNotFoundAsync(
            await UpdateAsync(id, intruder, update),
            await UpdateAsync(missing, intruder, update));

        var task = await (await _client.GetAsync($"/api/tasks/{id}", owner)).ReadJsonAsync();
        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
    }

    private async Task<Guid> CreateAsync(string token) =>
        (await _client.CreateTaskAndReadAsync(token, new { title = "Prepare demo", description = "Slides", dueDate = "2026-12-01" }))
            .GetProperty("id").GetGuid();

    private Task<HttpResponseMessage> UpdateAsync(Guid id, string token, object body) =>
        _client.SendAsync(HttpMethod.Put, $"/api/tasks/{id}", token, body);

    internal static async Task ShouldBeSameNotFoundAsync(HttpResponseMessage foreign, HttpResponseMessage missing)
    {
        foreign.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var foreignBody = await foreign.ReadJsonAsync();
        var missingBody = await missing.ReadJsonAsync();
        foreignBody.GetProperty("title").GetString().ShouldBe("Task not found");
        foreignBody.GetProperty("detail").GetString().ShouldBe("The task does not exist.");
        Comparable(foreignBody).ShouldBe(Comparable(missingBody));
    }

    // Everything but the per-request trace id must be identical.
    private static string Comparable(JsonElement problem) =>
        JsonSerializer.Serialize(problem.EnumerateObject()
            .Where(property => property.Name != "traceId")
            .ToDictionary(property => property.Name, property => property.Value.ToString()));
}
