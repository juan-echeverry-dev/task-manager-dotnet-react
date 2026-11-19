using System.Net;

using TaskLane.Api.Tests.Identity;

namespace TaskLane.Api.Tests.Tasks;

// Soft-delete behavior was replaced by permanent deletion (spec 003, DeleteTaskTests). The remaining
// case moves to RemovedEndpointsTests when the deleted-tasks list is removed (spec 003 US2).
public sealed class DeleteAndRestoreTaskTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ActiveList_RejectsSortingByDeletionDate()
    {
        var (_, token) = await _client.RegisterAndSignInAsync();

        var response = await _client.GetAsync("/api/tasks?sort=deletedAt", token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errors").GetProperty("sort")[0].GetString()
            .ShouldBe("Sort must be one of: dueDate, createdAt, title.");
    }
}
