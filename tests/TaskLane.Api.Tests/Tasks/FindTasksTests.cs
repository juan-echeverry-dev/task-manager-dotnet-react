using System.Net;

using Microsoft.Extensions.Options;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

public sealed class FindTasksTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task EachQueryParameter_NarrowsOrOrdersTheList()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        await CreateAsync(token, "Prepare demo", "InProgress", "2026-12-10");
        await CreateAsync(token, "Write notes", "ToDo", "2026-12-05", description: "for the DEMO");
        await CreateAsync(token, "Book room", "Done", "2027-02-01");
        await CreateAsync(token, "Someday", "ToDo", null);

        (await TitlesAsync(token, "search=demo")).ShouldBe(["Write notes", "Prepare demo"]);
        (await TitlesAsync(token, "status=Done")).ShouldBe(["Book room"]);
        (await TitlesAsync(token, "dueFrom=2026-12-01&dueTo=2026-12-31")).ShouldBe(["Write notes", "Prepare demo"]);
        (await TitlesAsync(token, "sort=title")).ShouldBe(["Book room", "Prepare demo", "Someday", "Write notes"]);
        (await TitlesAsync(token, "sort=title&direction=desc")).ShouldBe(["Write notes", "Someday", "Prepare demo", "Book room"]);
        (await TitlesAsync(token, "sort=createdAt")).ShouldBe(["Someday", "Book room", "Write notes", "Prepare demo"]);
        (await TitlesAsync(token, "direction=desc")).ShouldBe(["Book room", "Prepare demo", "Write notes", "Someday"]);
        (await TitlesAsync(token, "search=demo&status=ToDo&dueFrom=2026-12-01&dueTo=2026-12-31&sort=title&page=1&pageSize=5"))
            .ShouldBe(["Write notes"]);
    }

    [Fact]
    public async Task DeletedList_SupportsTheSameParameters()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        var demo = await CreateAsync(token, "Prepare demo", "Done", "2026-12-10");
        var other = await CreateAsync(token, "Write notes", "ToDo", "2026-12-05");
        await _client.SendAsync(HttpMethod.Delete, $"/api/tasks/{demo}", token);
        await _client.SendAsync(HttpMethod.Delete, $"/api/tasks/{other}", token);

        var page = await (await _client.GetAsync(
            "/api/tasks/deleted?search=demo&status=Done&dueFrom=2026-12-01&dueTo=2026-12-31&sort=deletedAt&direction=asc", token))
            .ReadJsonAsync();

        page.Titles().ShouldBe(["Prepare demo"]);
    }

    [Theory]
    [InlineData("dueFrom=2026-01-01&dueTo=2026-04-02", "dueTo", "Choose a range of at most 90 days.")]
    [InlineData("dueFrom=2026-02-01&dueTo=2026-01-01", "dueTo", "The start date must be on or before the end date.")]
    [InlineData("dueFrom=2026-01-01", "dueTo", "Choose both a start and an end date.")]
    [InlineData("dueFrom=01-01-2026&dueTo=2026-01-02", "dueFrom", "Enter dates as YYYY-MM-DD.")]
    [InlineData("sort=priority", "sort", "Sort must be one of: dueDate, createdAt, title.")]
    [InlineData("status=Later", "status", "Status must be one of: ToDo, InProgress, Done.")]
    [InlineData("direction=up", "direction", "Direction must be asc or desc.")]
    public async Task InvalidParameters_Return400KeyedByParameter(string query, string key, string message)
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync($"/api/tasks?{query}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty(key)[0].GetString().ShouldBe(message);
    }

    [Fact]
    public async Task ARangeOfExactly90Days_IsAccepted()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        (await _client.GetAsync("/api/tasks?dueFrom=2026-01-01&dueTo=2026-04-01", token)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_ReturnTheConfiguredMaximumRange()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync("/api/tasks/settings", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadJsonAsync()).GetProperty("dueDateRangeMaxDays").GetInt32().ShouldBe(90);
        (await _client.GetAsync("/api/tasks/settings", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheMaximumRange_CanBeChangedByConfiguration()
    {
        await using var configured = new ApiFactory(new Dictionary<string, string?> { ["Tasks:DueDateRangeMaxDays"] = "30" });
        var client = configured.CreateClient();
        var (_, token) = await client.RegisterAndSignInAsync();

        var settings = await (await client.GetAsync("/api/tasks/settings", token)).ReadJsonAsync();
        var within = await client.GetAsync("/api/tasks?dueFrom=2026-01-01&dueTo=2026-01-31", token);
        var beyond = await client.GetAsync("/api/tasks?dueFrom=2026-01-01&dueTo=2026-02-01", token);

        settings.GetProperty("dueDateRangeMaxDays").GetInt32().ShouldBe(30);
        within.StatusCode.ShouldBe(HttpStatusCode.OK);
        beyond.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await beyond.ReadJsonAsync()).GetProperty("errors").GetProperty("dueTo")[0].GetString()
            .ShouldBe("Choose a range of at most 30 days.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("3651")]
    public async Task AnInvalidMaximumRange_PreventsStartup(string value)
    {
        await using var misconfigured = new ApiFactory(new Dictionary<string, string?> { ["Tasks:DueDateRangeMaxDays"] = value });

        var exception = Should.Throw<OptionsValidationException>(() => misconfigured.CreateClient());

        exception.Message.ShouldContain("Tasks:DueDateRangeMaxDays");
    }

    private async Task<Guid> CreateAsync(string token, string title, string status, string? dueDate, string? description = null)
    {
        var task = await _client.CreateTaskAndReadAsync(token, new { title, description, status, dueDate });
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(1));
        return task.GetProperty("id").GetGuid();
    }

    private async Task<string[]> TitlesAsync(string token, string query)
    {
        var response = await _client.GetAsync($"/api/tasks?{query}", token);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.ReadJsonAsync()).Titles();
    }
}
