using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.ConnectCore.Models.Api.V1.Requests.Task;
using VRChatContentPublisher.Core.Models.VRChatApi.Rest.UnityPackages;
using VRChatContentPublisher.Core.Services.UserSession;

namespace VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

public sealed partial class HomeManualUploadPageViewModel(
    ManualPublishApiService manualPublishApiService,
    UserSessionManagerService userSessionManagerService,
    DialogService dialogService,
    ConfirmWorldSignatureDialogViewModelFactory confirmWorldSignatureDialogViewModelFactory)
    : PageViewModelBase
{
    private static readonly string[] KnownUnityVersions =
    [
        "2022.3.22f1",
        "2022.3.6f1",
        "2019.4.31f1",
        "2019.4.29f1"
    ];

    private static readonly string[] KnownPlatforms =
    [
        "standalonewindows",
        "android",
        "ios"
    ];

    private static readonly string[] KnownReleaseStatuses =
    [
        "private",
        "public",
        "hidden"
    ];

    public IReadOnlyList<string> UnityVersionCandidates { get; } = KnownUnityVersions;
    public IReadOnlyList<string> PlatformCandidates { get; } = KnownPlatforms;
    public IReadOnlyList<string> ReleaseStatusCandidates { get; } = KnownReleaseStatuses;

    [ObservableProperty] public partial string WorldBundlePath { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldThumbnailPath { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldId { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldName { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldPlatform { get; set; } = KnownPlatforms[0];
    [ObservableProperty] public partial string SelectedWorldUnityVersion { get; set; } = KnownUnityVersions[0];
    [ObservableProperty] public partial bool UseCustomWorldUnityVersion { get; set; }
    [ObservableProperty] public partial string CustomWorldUnityVersion { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldSignature { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsInitialWorldCreation { get; set; }

    [ObservableProperty]
    public partial List<ManualAuthorAccountOption> WorldAuthorAccounts { get; private set; } = [];

    [ObservableProperty] public partial ManualAuthorAccountOption? SelectedWorldAuthorAccount { get; set; }
    [ObservableProperty] public partial string WorldDescription { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldTags { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldReleaseStatus { get; set; } = "private";
    [ObservableProperty] public partial string WorldCapacity { get; set; } = "32";
    [ObservableProperty] public partial string WorldRecommendedCapacity { get; set; } = "16";
    [ObservableProperty] public partial string WorldPreviewYoutubeId { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldUdonProducts { get; set; } = string.Empty;
    [ObservableProperty] public partial string WorldValidationMessage { get; set; } = "Ready.";
    [ObservableProperty] public partial bool IsWorldSubmitting { get; set; }

    [ObservableProperty]
    public partial List<ManualExistingWorldOption> ExistingWorlds { get; private set; } = [];

    [ObservableProperty] public partial ManualExistingWorldOption? SelectedExistingWorld { get; set; }
    [ObservableProperty] public partial string ExistingWorldsStatusMessage { get; set; } = "Click refresh to load worlds.";
    [ObservableProperty] public partial bool IsLoadingExistingWorlds { get; set; }
    [ObservableProperty] public partial bool IsWorldUploadExpanded { get; set; }

    [ObservableProperty] public partial string AvatarBundlePath { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarThumbnailPath { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarId { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarName { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarPlatform { get; set; } = KnownPlatforms[0];
    [ObservableProperty] public partial string SelectedAvatarUnityVersion { get; set; } = KnownUnityVersions[0];
    [ObservableProperty] public partial bool UseCustomAvatarUnityVersion { get; set; }
    [ObservableProperty] public partial string CustomAvatarUnityVersion { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsInitialAvatarCreation { get; set; }
    [ObservableProperty] public partial string AvatarDescription { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarTags { get; set; } = string.Empty;
    [ObservableProperty] public partial string AvatarReleaseStatus { get; set; } = "private";
    [ObservableProperty] public partial string AvatarValidationMessage { get; set; } = "Ready.";
    [ObservableProperty] public partial bool IsAvatarSubmitting { get; set; }

    [ObservableProperty]
    public partial List<ManualExistingAvatarOption> ExistingAvatars { get; private set; } = [];

    [ObservableProperty] public partial ManualExistingAvatarOption? SelectedExistingAvatar { get; set; }
    [ObservableProperty] public partial string ExistingAvatarsStatusMessage { get; set; } = "Click refresh to load avatars.";
    [ObservableProperty] public partial bool IsLoadingExistingAvatars { get; set; }
    [ObservableProperty] public partial bool IsAvatarUploadExpanded { get; set; }

    [RelayCommand]
    private void Load()
    {
        RefreshWorldAuthorAccounts();
        userSessionManagerService.SessionCreated += OnSessionCreated;
        userSessionManagerService.SessionRemoved += OnSessionRemoved;
    }

    [RelayCommand]
    private void Unload()
    {
        userSessionManagerService.SessionCreated -= OnSessionCreated;
        userSessionManagerService.SessionRemoved -= OnSessionRemoved;
    }

    [RelayCommand]
    private async Task SubmitWorldAsync()
    {
        if (IsWorldSubmitting)
            return;

        IsWorldSubmitting = true;
        try
        {
            if (string.IsNullOrWhiteSpace(WorldSignature))
            {
                var signatureDecision = await PromptWorldSignatureDecisionAsync();
                if (signatureDecision == WorldSignatureDecision.CancelUpload)
                {
                    WorldValidationMessage = "Upload cancelled.";
                    return;
                }

                if (signatureDecision == WorldSignatureDecision.Compute)
                    await ComputeWorldSignatureCoreAsync();
            }

            RefreshWorldAuthorAccounts();
            var request = BuildWorldRequest();

            WorldValidationMessage = "Uploading world bundle...";
            request.WorldBundleFileId = await manualPublishApiService.UploadFileAsync(WorldBundlePath);

            if (!string.IsNullOrWhiteSpace(WorldThumbnailPath))
            {
                WorldValidationMessage = "Uploading world thumbnail...";
                request.ThumbnailFileId = await manualPublishApiService.UploadFileAsync(WorldThumbnailPath);
            }

            WorldValidationMessage = "Creating world publish task...";
            await manualPublishApiService.CreateWorldTaskAsync(request);

            WorldValidationMessage = "World publish task submitted successfully.";
        }
        catch (Exception ex)
        {
            WorldValidationMessage = ex.Message;
        }
        finally
        {
            IsWorldSubmitting = false;
        }
    }

    [RelayCommand]
    private async Task SubmitAvatarAsync()
    {
        if (IsAvatarSubmitting)
            return;

        IsAvatarSubmitting = true;
        try
        {
            var request = BuildAvatarRequest();

            AvatarValidationMessage = "Uploading avatar bundle...";
            request.AvatarBundleFileId = await manualPublishApiService.UploadFileAsync(AvatarBundlePath);

            if (!string.IsNullOrWhiteSpace(AvatarThumbnailPath))
            {
                AvatarValidationMessage = "Uploading avatar thumbnail...";
                request.ThumbnailFileId = await manualPublishApiService.UploadFileAsync(AvatarThumbnailPath);
            }

            AvatarValidationMessage = "Creating avatar publish task...";
            await manualPublishApiService.CreateAvatarTaskAsync(request);

            AvatarValidationMessage = "Avatar publish task submitted successfully.";
        }
        catch (Exception ex)
        {
            AvatarValidationMessage = ex.Message;
        }
        finally
        {
            IsAvatarSubmitting = false;
        }
    }

    [RelayCommand]
    private async Task ComputeWorldSignatureAsync()
    {
        try
        {
            await ComputeWorldSignatureCoreAsync();
        }
        catch (Exception ex)
        {
            WorldValidationMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void GenerateWorldId()
    {
        var generatedWorldId = $"wrld_{Guid.NewGuid():D}";
        WorldId = generatedWorldId;
        WorldValidationMessage = $"Generated world id: {generatedWorldId}";
    }

    [RelayCommand]
    private async Task RefreshExistingWorldsAsync()
    {
        if (IsLoadingExistingWorlds)
            return;

        IsLoadingExistingWorlds = true;
        try
        {
            RefreshWorldAuthorAccounts();
            var session = GetSelectedLoggedInSession();

            ExistingWorldsStatusMessage = "Loading worlds from selected account...";
            var worlds = await session.GetApiClient().GetMyWorldsAsync();
            var options = worlds
                .Select(MapWorldOption)
                .OrderBy(w => w.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ExistingWorlds = options;
            SelectedExistingWorld = options.FirstOrDefault();

            ExistingWorldsStatusMessage = options.Count == 0
                ? "No worlds found under current account."
                : $"Loaded {options.Count} worlds.";
        }
        catch (Exception ex)
        {
            ExistingWorlds = [];
            SelectedExistingWorld = null;
            ExistingWorldsStatusMessage = ex.Message;
        }
        finally
        {
            IsLoadingExistingWorlds = false;
        }
    }

    [RelayCommand]
    private void ApplySelectedWorld()
    {
        if (SelectedExistingWorld is null)
        {
            WorldValidationMessage = "Select a world from the account list first.";
            return;
        }

        WorldId = SelectedExistingWorld.Id;
        WorldName = SelectedExistingWorld.Name;

        if (!string.IsNullOrWhiteSpace(SelectedExistingWorld.Platform))
            WorldPlatform = SelectedExistingWorld.Platform;

        ApplyWorldUnityVersion(SelectedExistingWorld.UnityVersion);
        IsInitialWorldCreation = false;
        WorldValidationMessage = $"Applied world {SelectedExistingWorld.Id} to manual upload form.";
    }

    [RelayCommand]
    private async Task RefreshExistingAvatarsAsync()
    {
        if (IsLoadingExistingAvatars)
            return;

        IsLoadingExistingAvatars = true;
        try
        {
            RefreshWorldAuthorAccounts();
            var session = GetSelectedLoggedInSession();

            ExistingAvatarsStatusMessage = "Loading avatars from selected account...";
            var avatars = await session.GetApiClient().GetMyAvatarsAsync();
            var options = avatars
                .Select(MapAvatarOption)
                .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            ExistingAvatars = options;
            SelectedExistingAvatar = options.FirstOrDefault();

            ExistingAvatarsStatusMessage = options.Count == 0
                ? "No avatars found under current account."
                : $"Loaded {options.Count} avatars.";
        }
        catch (Exception ex)
        {
            ExistingAvatars = [];
            SelectedExistingAvatar = null;
            ExistingAvatarsStatusMessage = ex.Message;
        }
        finally
        {
            IsLoadingExistingAvatars = false;
        }
    }

    [RelayCommand]
    private void ApplySelectedAvatar()
    {
        if (SelectedExistingAvatar is null)
        {
            AvatarValidationMessage = "Select an avatar from the account list first.";
            return;
        }

        AvatarId = SelectedExistingAvatar.Id;
        AvatarName = SelectedExistingAvatar.Name;

        if (!string.IsNullOrWhiteSpace(SelectedExistingAvatar.Platform))
            AvatarPlatform = SelectedExistingAvatar.Platform;

        ApplyAvatarUnityVersion(SelectedExistingAvatar.UnityVersion);
        IsInitialAvatarCreation = false;
        AvatarValidationMessage = $"Applied avatar {SelectedExistingAvatar.Id} to manual upload form.";
    }

    [RelayCommand]
    private void ClearWorldReleaseStatus()
    {
        WorldReleaseStatus = string.Empty;
    }

    [RelayCommand]
    private void ClearAvatarReleaseStatus()
    {
        AvatarReleaseStatus = string.Empty;
    }

    private CreateWorldPublishTaskRequest BuildWorldRequest()
    {
        ValidateRequired(WorldBundlePath, nameof(WorldBundlePath));
        ValidateRequired(WorldName, nameof(WorldName));
        ValidateRequired(WorldPlatform, nameof(WorldPlatform));

        ValidateFileExists(WorldBundlePath, nameof(WorldBundlePath));
        ValidateFileExtension(WorldBundlePath, ".vrcw", nameof(WorldBundlePath));
        ValidateOptionalFileExists(WorldThumbnailPath, nameof(WorldThumbnailPath));

        if (IsInitialWorldCreation)
        {
            ValidateRequired(WorldThumbnailPath, nameof(WorldThumbnailPath));
            ValidateRequired(WorldDescription, nameof(WorldDescription));
            ValidateRequired(WorldReleaseStatus, nameof(WorldReleaseStatus));
        }

        var worldAuthorId = SelectedWorldAuthorAccount?.UserId;
        if (string.IsNullOrWhiteSpace(worldAuthorId))
            throw new ArgumentException("Please select a logged-in account as world author.");

        var capacity = IsInitialWorldCreation
            ? ParseRequiredPositiveInt(WorldCapacity, nameof(WorldCapacity))
            : ParseOptionalPositiveInt(WorldCapacity, nameof(WorldCapacity));

        var recommendedCapacity = IsInitialWorldCreation
            ? ParseRequiredPositiveInt(WorldRecommendedCapacity, nameof(WorldRecommendedCapacity))
            : ParseOptionalPositiveInt(WorldRecommendedCapacity, nameof(WorldRecommendedCapacity));

        if (capacity is not null && recommendedCapacity is not null && recommendedCapacity > capacity)
            throw new ArgumentException("WorldRecommendedCapacity cannot be greater than WorldCapacity.");

        var worldId = ResolveWorldId();

        var request = new CreateWorldPublishTaskRequest
        {
            WorldId = worldId,
            Name = WorldName.Trim(),
            WorldBundleFileId = string.Empty,
            Platform = WorldPlatform.Trim(),
            UnityVersion = ResolveWorldUnityVersion(),
            AuthorId = worldAuthorId,
            WorldSignature = NullIfWhiteSpace(WorldSignature),
            ThumbnailFileId = null,
            Description = NullIfWhiteSpace(WorldDescription),
            Tags = ParseCsv(WorldTags),
            ReleaseStatus = NullIfWhiteSpace(WorldReleaseStatus),
            Capacity = capacity,
            RecommendedCapacity = recommendedCapacity,
            PreviewYoutubeId = NullIfWhiteSpace(WorldPreviewYoutubeId),
            UdonProducts = ParseOptionalCsv(WorldUdonProducts)
        };

        return request;
    }

    private CreateAvatarPublishTaskRequest BuildAvatarRequest()
    {
        ValidateRequired(AvatarBundlePath, nameof(AvatarBundlePath));
        ValidateRequired(AvatarName, nameof(AvatarName));
        ValidateRequired(AvatarPlatform, nameof(AvatarPlatform));

        ValidateFileExists(AvatarBundlePath, nameof(AvatarBundlePath));
        ValidateFileExtension(AvatarBundlePath, ".vrca", nameof(AvatarBundlePath));
        ValidateOptionalFileExists(AvatarThumbnailPath, nameof(AvatarThumbnailPath));

        if (IsInitialAvatarCreation)
            ValidateRequired(AvatarThumbnailPath, nameof(AvatarThumbnailPath));

        var avatarAuthorId = SelectedWorldAuthorAccount?.UserId;
        if (string.IsNullOrWhiteSpace(avatarAuthorId))
            throw new ArgumentException("Please select a logged-in account for avatar publishing.");

        var avatarId = ResolveAvatarId();

        var request = new CreateAvatarPublishTaskRequest
        {
            AvatarId = avatarId,
            Name = AvatarName.Trim(),
            AvatarBundleFileId = string.Empty,
            Platform = AvatarPlatform.Trim(),
            UnityVersion = ResolveAvatarUnityVersion(),
            AuthorId = avatarAuthorId,
            ThumbnailFileId = null,
            Description = NullIfWhiteSpace(AvatarDescription),
            Tags = ParseCsv(AvatarTags),
            ReleaseStatus = NullIfWhiteSpace(AvatarReleaseStatus)
        };

        return request;
    }

    private string ResolveWorldId()
    {
        ValidateRequired(WorldId, nameof(WorldId));
        return WorldId.Trim();
    }

    private string ResolveAvatarId()
    {
        if (!string.IsNullOrWhiteSpace(AvatarId))
            return AvatarId.Trim();

        if (!IsInitialAvatarCreation)
            throw new ArgumentException($"{nameof(AvatarId)} is required.");

        var generatedAvatarId = $"avtr_{Guid.NewGuid():D}";
        AvatarId = generatedAvatarId;
        AvatarValidationMessage = $"Generated avatar id for initial creation: {generatedAvatarId}";
        return generatedAvatarId;
    }

    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.");
    }

    private static void ValidateFileExists(string path, string fieldName)
    {
        var trimmedPath = path.Trim();
        if (!File.Exists(trimmedPath))
            throw new ArgumentException($"{fieldName} path does not exist: {trimmedPath}");
    }

    private static void ValidateOptionalFileExists(string path, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        ValidateFileExists(path, fieldName);
    }

    private static void ValidateFileExtension(string path, string extension, string fieldName)
    {
        var actual = Path.GetExtension(path.Trim());
        if (!string.Equals(actual, extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"{fieldName} must be a {extension} file.");
    }

    private static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<WorldSignatureDecision> PromptWorldSignatureDecisionAsync()
    {
        var dialog = confirmWorldSignatureDialogViewModelFactory.Create();
        var result = await dialogService.ShowDialogAsync(dialog);
        return result switch
        {
            true => WorldSignatureDecision.Compute,
            false => WorldSignatureDecision.ContinueWithoutSignature,
            _ => WorldSignatureDecision.CancelUpload
        };
    }

    private async Task ComputeWorldSignatureCoreAsync()
    {
        ValidateRequired(WorldBundlePath, nameof(WorldBundlePath));
        ValidateFileExists(WorldBundlePath, nameof(WorldBundlePath));
        ValidateFileExtension(WorldBundlePath, ".vrcw", nameof(WorldBundlePath));

        await using var stream = File.OpenRead(WorldBundlePath.Trim());
        var hash = await SHA256.HashDataAsync(stream);
        WorldSignature = Convert.ToBase64String(hash);
        WorldValidationMessage = "World signature calculated from bundle file.";
    }

    partial void OnSelectedExistingWorldChanged(ManualExistingWorldOption? value)
    {
        if (value is null)
            return;

        ApplySelectedWorld();
    }

    partial void OnSelectedExistingAvatarChanged(ManualExistingAvatarOption? value)
    {
        if (value is null)
            return;

        ApplySelectedAvatar();
    }

    private enum WorldSignatureDecision
    {
        Compute,
        ContinueWithoutSignature,
        CancelUpload
    }

    private string ResolveWorldUnityVersion()
    {
        var version = UseCustomWorldUnityVersion ? CustomWorldUnityVersion : SelectedWorldUnityVersion;
        ValidateRequired(version, nameof(SelectedWorldUnityVersion));
        return version.Trim();
    }

    private string ResolveAvatarUnityVersion()
    {
        var version = UseCustomAvatarUnityVersion ? CustomAvatarUnityVersion : SelectedAvatarUnityVersion;
        ValidateRequired(version, nameof(SelectedAvatarUnityVersion));
        return version.Trim();
    }

    private static string[] ParseCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value
            .Split(',')
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[]? ParseOptionalCsv(string value)
    {
        var result = ParseCsv(value);
        return result.Length == 0 ? null : result;
    }

    private static int ParseRequiredPositiveInt(string value, string fieldName)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
            throw new ArgumentException($"{fieldName} must be a positive integer.");

        return parsed;
    }

    private static int? ParseOptionalPositiveInt(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return ParseRequiredPositiveInt(value, fieldName);
    }

    private void OnSessionCreated(object? _, UserSessionService __)
    {
        RefreshWorldAuthorAccounts();
    }

    private void OnSessionRemoved(object? _, UserSessionService __)
    {
        RefreshWorldAuthorAccounts();
    }

    private void RefreshWorldAuthorAccounts()
    {
        var oldSelectionUserId = SelectedWorldAuthorAccount?.UserId;

        var options = userSessionManagerService.Sessions
            .Where(s => s.State == UserSessionState.LoggedIn && !string.IsNullOrWhiteSpace(s.UserId))
            .Select(s =>
            {
                var displayName = s.CurrentUser?.DisplayName ?? s.UserNameOrEmail;
                return new ManualAuthorAccountOption(
                    s.UserId!,
                    $"{displayName} ({s.UserId})");
            })
            .OrderBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        WorldAuthorAccounts = options;
        SelectedWorldAuthorAccount = options.FirstOrDefault(a => a.UserId == oldSelectionUserId) ??
                                     options.FirstOrDefault();
    }

    private UserSessionService GetSelectedLoggedInSession()
    {
        var userId = SelectedWorldAuthorAccount?.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Please select a logged-in account first.");

        var session = userSessionManagerService.Sessions.FirstOrDefault(s =>
            s.State == UserSessionState.LoggedIn &&
            string.Equals(s.UserId, userId, StringComparison.Ordinal));

        if (session is null)
            throw new InvalidOperationException("The selected account is no longer logged in. Please relogin.");

        return session;
    }

    private static ManualExistingWorldOption MapWorldOption(Core.Models.VRChatApi.Rest.Worlds.VRChatApiWorld world)
    {
        var package = PickPreferredUnityPackage(world.UnityPackages);
        var platform = package?.Platform;
        var unityVersion = package?.UnityVersion;

        var displayParts = new List<string> { world.Name, $"({world.Id})" };
        if (!string.IsNullOrWhiteSpace(platform) || !string.IsNullOrWhiteSpace(unityVersion))
            displayParts.Add($"[{platform ?? "?"} / {unityVersion ?? "?"}]");

        return new ManualExistingWorldOption(
            world.Id,
            world.Name,
            platform,
            unityVersion,
            string.Join(' ', displayParts));
    }

    private static ManualExistingAvatarOption MapAvatarOption(Core.Models.VRChatApi.Rest.Avatars.VRChatApiAvatar avatar)
    {
        var package = PickPreferredUnityPackage(avatar.UnityPackages);
        var platform = package?.Platform;
        var unityVersion = package?.UnityVersion;

        var displayParts = new List<string> { avatar.Name, $"({avatar.Id})" };
        if (!string.IsNullOrWhiteSpace(platform) || !string.IsNullOrWhiteSpace(unityVersion))
            displayParts.Add($"[{platform ?? "?"} / {unityVersion ?? "?"}]");

        return new ManualExistingAvatarOption(
            avatar.Id,
            avatar.Name,
            platform,
            unityVersion,
            string.Join(' ', displayParts));
    }

    private static VRChatApiUnityPackage? PickPreferredUnityPackage(IReadOnlyList<VRChatApiUnityPackage> packages)
    {
        if (packages.Count == 0)
            return null;

        return packages.FirstOrDefault(p => string.Equals(
                   p.Platform,
                   "standalonewindows",
                   StringComparison.OrdinalIgnoreCase))
               ?? packages[0];
    }

    private void ApplyWorldUnityVersion(string? unityVersion)
    {
        if (string.IsNullOrWhiteSpace(unityVersion))
            return;

        var knownVersion = KnownUnityVersions.FirstOrDefault(v =>
            string.Equals(v, unityVersion, StringComparison.OrdinalIgnoreCase));

        if (knownVersion is not null)
        {
            UseCustomWorldUnityVersion = false;
            SelectedWorldUnityVersion = knownVersion;
            CustomWorldUnityVersion = string.Empty;
            return;
        }

        UseCustomWorldUnityVersion = true;
        CustomWorldUnityVersion = unityVersion.Trim();
    }

    private void ApplyAvatarUnityVersion(string? unityVersion)
    {
        if (string.IsNullOrWhiteSpace(unityVersion))
            return;

        var knownVersion = KnownUnityVersions.FirstOrDefault(v =>
            string.Equals(v, unityVersion, StringComparison.OrdinalIgnoreCase));

        if (knownVersion is not null)
        {
            UseCustomAvatarUnityVersion = false;
            SelectedAvatarUnityVersion = knownVersion;
            CustomAvatarUnityVersion = string.Empty;
            return;
        }

        UseCustomAvatarUnityVersion = true;
        CustomAvatarUnityVersion = unityVersion.Trim();
    }

    partial void OnSelectedWorldAuthorAccountChanged(ManualAuthorAccountOption? value)
    {
        ExistingWorlds = [];
        SelectedExistingWorld = null;
        ExistingWorldsStatusMessage = "Account changed. Click refresh to load worlds.";

        ExistingAvatars = [];
        SelectedExistingAvatar = null;
        ExistingAvatarsStatusMessage = "Account changed. Click refresh to load avatars.";
    }
}

public sealed record ManualAuthorAccountOption(string UserId, string DisplayName);

public sealed record ManualExistingWorldOption(
    string Id,
    string Name,
    string? Platform,
    string? UnityVersion,
    string DisplayName);

public sealed record ManualExistingAvatarOption(
    string Id,
    string Name,
    string? Platform,
    string? UnityVersion,
    string DisplayName);
