using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskLane.Api.Tests.Tasks;

/// <summary>Drives the Tasks endpoints the way the SPA does, with a bearer token.</summary>
internal static class TasksClient
{
    public static async Task<HttpResponseMessage> SendAsync(
        this HttpClient client, HttpMethod method, string path, string? accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        // Awaited here: the request (and its content) must outlive the call.
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> CreateTaskAsync(this HttpClient client, string? accessToken, object body) =>
        client.SendAsync(HttpMethod.Post, "/api/tasks", accessToken, body);

    public static Task<HttpResponseMessage> GetAsync(this HttpClient client, string path, string? accessToken) =>
        client.SendAsync(HttpMethod.Get, path, accessToken);

    /// <summary>Creates a task and returns its JSON representation.</summary>
    public static async Task<JsonElement> CreateTaskAndReadAsync(this HttpClient client, string accessToken, object body)
    {
        var response = await client.CreateTaskAsync(accessToken, body);
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync();
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        return document.RootElement.Clone();
    }

    public static string[] Titles(this JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(task => task.GetProperty("title").GetString()!)];
}
