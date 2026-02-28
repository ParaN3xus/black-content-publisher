using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using VRChatContentPublisher.App.ViewModels.Pages.HomeTab;

namespace VRChatContentPublisher.App.Pages.HomeTab;

public partial class HomeManualUploadPage : UserControl
{
    public HomeManualUploadPage()
    {
        InitializeComponent();
    }

    private void OnFileDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void OnWorldBundleDrop(object? sender, DragEventArgs e)
    {
        SetDroppedPath(e,
            expectedExtension: ".vrcw",
            assignPath: (vm, path) => vm.WorldBundlePath = path,
            setMessage: (vm, message) => vm.WorldValidationMessage = message);
    }

    private void OnWorldThumbnailDrop(object? sender, DragEventArgs e)
    {
        SetDroppedPath(e,
            expectedExtension: null,
            assignPath: (vm, path) => vm.WorldThumbnailPath = path,
            setMessage: (vm, message) => vm.WorldValidationMessage = message);
    }

    private void OnAvatarBundleDrop(object? sender, DragEventArgs e)
    {
        SetDroppedPath(e,
            expectedExtension: ".vrca",
            assignPath: (vm, path) => vm.AvatarBundlePath = path,
            setMessage: (vm, message) => vm.AvatarValidationMessage = message);
    }

    private void OnAvatarThumbnailDrop(object? sender, DragEventArgs e)
    {
        SetDroppedPath(e,
            expectedExtension: null,
            assignPath: (vm, path) => vm.AvatarThumbnailPath = path,
            setMessage: (vm, message) => vm.AvatarValidationMessage = message);
    }

    private void SetDroppedPath(
        DragEventArgs e,
        string? expectedExtension,
        Action<HomeManualUploadPageViewModel, string> assignPath,
        Action<HomeManualUploadPageViewModel, string> setMessage)
    {
        e.Handled = true;

        if (DataContext is not HomeManualUploadPageViewModel viewModel)
            return;

        if (!TryGetLocalFilePath(e, out var path))
        {
            setMessage(viewModel, "Drop failed: no local file detected.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(expectedExtension) &&
            !string.Equals(Path.GetExtension(path), expectedExtension, StringComparison.OrdinalIgnoreCase))
        {
            setMessage(viewModel, $"Drop failed: file must end with {expectedExtension}.");
            return;
        }

        assignPath(viewModel, path);
    }

    private static bool TryGetLocalFilePath(DragEventArgs e, out string path)
    {
        path = string.Empty;

        var files = e.Data.GetFiles();
        if (files is null)
            return false;

        path = files
            .OfType<IStorageFile>()
            .Select(file => file.TryGetLocalPath())
            .FirstOrDefault(localPath => !string.IsNullOrWhiteSpace(localPath))
            ?? string.Empty;

        return !string.IsNullOrWhiteSpace(path);
    }
}
