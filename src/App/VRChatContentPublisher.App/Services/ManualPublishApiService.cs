using System.Net.Http.Json;
using System.Text.Json;
using VRChatContentPublisher.ConnectCore.Models.Api.V1;
using VRChatContentPublisher.ConnectCore.Models.Api.V1.Requests.Task;

namespace VRChatContentPublisher.App.Services;

public sealed class ManualPublishApiService(HttpClient httpClient)
{
    public async ValueTask<string> UploadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(fileStream);
        using var formContent = new MultipartFormDataContent
        {
            { fileContent, "file", Path.GetFileName(filePath) }
        };

        var response = await httpClient.PostAsync("files", formContent, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync(
            ApiV1JsonContext.Default.ApiV1UploadFileResponse,
            cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.FileId))
            throw new InvalidOperationException("Upload API returned an invalid file id.");

        return payload.FileId;
    }

    public async ValueTask CreateWorldTaskAsync(CreateWorldPublishTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "tasks/world",
            JsonContent.Create(request, ApiV1JsonContext.Default.CreateWorldPublishTaskRequest),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async ValueTask CreateAvatarTaskAsync(CreateAvatarPublishTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            "tasks/avatar",
            JsonContent.Create(request, ApiV1JsonContext.Default.CreateAvatarPublishTaskRequest),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async ValueTask EnsureSuccessAsync(HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                if (jsonDoc.RootElement.TryGetProperty("detail", out var detailElement) &&
                    detailElement.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(detailElement.GetString()))
                {
                    throw new HttpRequestException(detailElement.GetString());
                }
            }
            catch (JsonException)
            {
                // Ignore invalid json and throw fallback error.
            }
        }

        throw new HttpRequestException(
            $"API request failed with status {(int)response.StatusCode} ({response.StatusCode}).");
    }
}
