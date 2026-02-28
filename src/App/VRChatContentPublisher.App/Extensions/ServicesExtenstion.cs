using Microsoft.Extensions.DependencyInjection;
using VRChatContentPublisher.App.Services;
using VRChatContentPublisher.App.ViewModels;
using VRChatContentPublisher.App.ViewModels.Data;
using VRChatContentPublisher.App.ViewModels.Data.PublishTasks;
using VRChatContentPublisher.App.ViewModels.Dialogs;
using VRChatContentPublisher.App.ViewModels.NetworkDiagnostic;
using VRChatContentPublisher.App.ViewModels.Pages;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;
using VRChatContentPublisher.App.ViewModels.Pages.Settings;
using VRChatContentPublisher.App.ViewModels.Settings;
using VRChatContentPublisher.ConnectCore.Services.Connect.Challenge;
using VRChatContentPublisher.IpcCore.Services;

namespace VRChatContentPublisher.App.Extensions;

public static class ServicesExtenstion
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<AppWebImageLoader>();
        services.AddHttpClient<ManualPublishApiService>(client =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:59328/v1/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddSingleton<AppWindowService>();
        services.AddSingleton<IActivateWindowService>(s => s.GetRequiredService<AppWindowService>());

        // Dialog
        services.AddSingleton<DialogService>();

        // Dialogs
        services.AddTransient<TwoFactorAuthDialogViewModelFactory>();
        services.AddTransient<ExitAppDialogViewModel>();
        services.AddTransient<ConfirmWorldSignatureDialogViewModelFactory>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<TaskErrorReportWindowViewModel>();
        services.AddTransient<NetworkDiagnosticWindowViewModel>();

        services.AddSingleton<NavigationService>();

        services.AddTransient<BootstrapPageViewModel>();

        services.AddSingleton<HomePageViewModel>();
        services.AddTransient<SettingsPageViewModel>();

        // Data ViewModels
        services.AddTransient<UserSessionViewModelFactory>();
        services.AddTransient<PublishTaskViewModelFactory>();
        services.AddTransient<PublishTaskManagerViewModelFactory>();
        services.AddTransient<InvalidSessionTaskManagerViewModelFactory>();
        services.AddTransient<PublishTaskManagerContainerViewModelFactory>();

        // HomePage Tabs
        services.AddSingleton<HomeTasksPageViewModel>();
        services.AddSingleton<HomeManualUploadPageViewModel>();

        // Settings Pages
        services.AddTransient<AddAccountPageViewModelFactory>();
        services.AddTransient<SettingsFixAccountPageViewModelFactory>();
        services.AddTransient<LicensePageViewModel>();

        // Settings Sections
        services.AddTransient<AccountsSettingsViewModel>();
        services.AddTransient<HttpProxySettingsViewModel>();
        services.AddTransient<AboutSettingsViewModel>();
        services.AddTransient<DebugSettingsViewModel>();

        // Connect Core
        services.AddSingleton<IRequestChallengeService, DefaultRequestChallengeService>();

        return services;
    }
}
