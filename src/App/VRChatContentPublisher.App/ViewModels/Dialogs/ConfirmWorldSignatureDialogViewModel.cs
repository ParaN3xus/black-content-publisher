using CommunityToolkit.Mvvm.Input;

namespace VRChatContentPublisher.App.ViewModels.Dialogs;

public sealed partial class ConfirmWorldSignatureDialogViewModel : DialogViewModelBase
{
    [RelayCommand]
    private void Compute()
    {
        RequestClose(true);
    }

    [RelayCommand]
    private void ContinueWithoutSignature()
    {
        RequestClose(false);
    }

    [RelayCommand]
    private void CancelUpload()
    {
        RequestClose();
    }
}

public sealed class ConfirmWorldSignatureDialogViewModelFactory
{
    public ConfirmWorldSignatureDialogViewModel Create()
    {
        return new ConfirmWorldSignatureDialogViewModel();
    }
}
