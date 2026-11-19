using System.Net;
using System.Text.Json;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

public sealed class CreateAndListTasksTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_WithOnlyTitle_Returns201WithDefaults()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.CreateTaskAsync(token, new { title = "Prepare demo" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = await response.ReadJsonAsync();
        var id = task.GetProperty("id").GetGuid();
        response.Headers.Location!.OriginalString.ShouldBe($"/api/tasks/{id}");
        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
        task.GetProperty("description").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("status").GetString().ShouldBe("ToDo");
        task.GetProperty("dueDate").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("createdAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow());
        task.GetProperty("updatedAt").GetDateTimeOffset().ShouldBe(factory.TimeProvider.GetUtcNow());
        task.TryGetProperty("ownerId", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Create_WithAllValues_EchoesThem()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var task = await _client.CreateTaskAndReadAsync(token, new
        {
            title = "  Prepare demo  ",
            description = "Slides and seed data",
            status = "InProgress",
            dueDate = "2026-12-01",
        });

        task.GetProperty("title").GetString().ShouldBe("Prepare demo");
        task.GetProperty("description").GetString().ShouldBe("Slides and seed data");
        task.GetProperty("status").GetString().ShouldBe("InProgress");
        task.GetProperty("dueDate").GetString().ShouldBe("2026-12-01");
    }

    [Fact]
    public async Task Create_WithInvalidValues_Returns400WithEveryFieldError()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.CreateTaskAsync(token, new
        {
            title = " ",
            description = new string('d', 2001),
            status = "Later",
            dueDate = "2100-01-01",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var errors = (await response.ReadJsonAsync()).GetProperty("errors");
        errors.GetProperty("title")[0].GetString().ShouldBe("Title is required.");
        errors.GetProperty("description")[0].GetString().ShouldBe("Description must be at most 2,000 characters.");
        errors.GetProperty("status")[0].GetString().ShouldBe("Status must be one of: ToDo, InProgress, Done.");
        errors.GetProperty("dueDate")[0].GetString().ShouldBe("Enter a due date between 2000-01-01 and 2099-12-31.");
    }

    [Fact]
    public async Task Create_WithoutBodyValues_Returns400ForTitle()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.CreateTaskAsync(token, new { });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty("title")[0].GetString()
            .ShouldBe("Title is required.");
    }

    [Fact]
    public async Task List_ReturnsOwnTasksInDefaultOrderWithPaging()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        await _client.CreateTaskAndReadAsync(token, new { title = "No date" });
        await _client.CreateTaskAndReadAsync(token, new { title = "January", dueDate = "2027-01-01" });
        await _client.CreateTaskAndReadAsync(token, new { title = "December", dueDate = "2026-12-01" });

        var response = await _client.GetAsync("/api/tasks", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.ReadJsonAsync();
        page.Titles().ShouldBe(["December", "January", "No date"]);
        page.GetProperty("page").GetInt32().ShouldBe(1);
        page.GetProperty("pageSize").GetInt32().ShouldBe(20);
        page.GetProperty("totalCount").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task List_WithPageSize_ReturnsThatSlice()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        await _client.CreateTaskAndReadAsync(token, new { title = "First", dueDate = "2026-12-01" });
        await _client.CreateTaskAndReadAsync(token, new { title = "Second", dueDate = "2026-12-02" });
        await _client.CreateTaskAndReadAsync(token, new { title = "Third", dueDate = "2026-12-03" });

        var page = await (await _client.GetAsync("/api/tasks?page=2&pageSize=2", token)).ReadJsonAsync();

        page.Titles().ShouldBe(["Third"]);
        page.GetProperty("totalCount").GetInt32().ShouldBe(3);
    }

    // Regression: a page number whose offset overflows int returned the first page's items.
    [Fact]
    public async Task List_WithHugePageNumber_ReturnsAnEmptyPageWithTheRealTotal()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();
        await _client.CreateTaskAndReadAsync(token, new { title = "Only" });

        var response = await _client.GetAsync($"/api/tasks?page={int.MaxValue}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.ReadJsonAsync();
        page.Titles().ShouldBeEmpty();
        page.GetProperty("page").GetInt32().ShouldBe(int.MaxValue);
        page.GetProperty("totalCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task List_OfAnotherAccount_DoesNotIncludeMyTasks()
    {
        var (_, mine) = await _client.RegisterAndSignInAsync();
        await _client.CreateTaskAndReadAsync(mine, new { title = "Mine" });
        var (_, theirs) = await _client.RegisterAndSignInAsync();

        var page = await (await _client.GetAsync("/api/tasks", theirs)).ReadJsonAsync();

        page.Titles().ShouldBeEmpty();
        page.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        (await _client.GetAsync("/api/tasks", null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _client.CreateTaskAsync(null, new { title = "Prepare demo" })).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("pageSize=101", "pageSize", "Page size must be between 1 and 100.")]
    [InlineData("page=0", "page", "Page must be 1 or greater.")]
    public async Task List_WithInvalidPaging_Returns400KeyedByParameter(string query, string key, string message)
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync($"/api/tasks?{query}", token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty(key)[0].GetString().ShouldBe(message);
    }
}
