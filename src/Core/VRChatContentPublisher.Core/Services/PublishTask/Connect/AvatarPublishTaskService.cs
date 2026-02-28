using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VRChatContentPublisher.ConnectCore.Exceptions;
using VRChatContentPublisher.ConnectCore.Services.PublishTask;
using VRChatContentPublisher.Core.Models.VRChatApi;
using VRChatContentPublisher.Core.Services.PublishTask.ContentPublisher;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.Core.Services.PublishTask.Connect;

public sealed class AvatarPublishTaskService(
    AvatarContentPublisherFactory contentPublisherFactory,
    UserSessionManagerService userSessionManagerService,
    ILogger<AvatarPublishTaskService> logger)
    : IAvatarPublishTaskService
{
    public async Task CreatePublishTaskAsync(string avatarId,
        string avatarBundleFileId,
        string name,
        string platform,
        string unityVersion,
        string? authorId,
        string? thumbnailFileId,
        string? description,
        string[]? tags,
        string? releaseStatus)
    {
        try
        {
            var userSession = await GetUserSessionByAvatarIdAsync(avatarId, authorId);
            var scope = await userSession.CreateOrGetSessionScopeAsync();

            var taskManager = scope.ServiceProvider.GetRequiredService<TaskManagerService>();
            var contentPublisher =
                contentPublisherFactory.Create(userSession, avatarId, name, platform, unityVersion);

            var task = await taskManager.CreateTask(avatarId, avatarBundleFileId, thumbnailFileId, description, tags,
                releaseStatus, contentPublisher);
            task.Start();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create avatar publish task for avatar {AvatarId}", avatarId);
            throw;
        }
    }

    public async ValueTask<UserSessionService> GetUserSessionByAvatarIdAsync(string avatarId, string? requestUserId = null)
    {
        if (!userSessionManagerService.IsAnySessionAvailable)
            throw new NoUserSessionAvailableException();

        if (requestUserId is not null)
        {
            if (userSessionManagerService.Sessions
                    .FirstOrDefault(session => session.UserId == requestUserId) is not { } requestSession)
                throw new ArgumentException("The specified user session was not found.", nameof(requestUserId));

            try
            {
                var avatar = await requestSession.GetApiClient().GetAvatarAsync(avatarId);
                if (avatar.AuthorId != requestUserId)
                    throw new ArgumentException("The specified user does not own the avatar.", nameof(requestUserId));
            }
            catch (ApiErrorException ex) when (ex.StatusCode == 404)
            {
                // If the avatar does not exist, continue with the selected account and create it.
                return requestSession;
            }

            return requestSession;
        }

        foreach (var session in userSessionManagerService.Sessions)
        {
            try
            {
                var avatar = await session.GetApiClient().GetAvatarAsync(avatarId);
                if (avatar.AuthorId != session.UserId)
                    continue;

                return session;
            }
            catch
            {
                // ignored
            }
        }

        throw new ContentOwnerSessionOrAvatarNotFoundException();
    }
}
