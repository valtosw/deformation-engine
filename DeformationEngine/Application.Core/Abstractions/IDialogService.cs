namespace Application.Core.Abstractions
{
    public interface IDialogService
    {
        bool ShowConfirmation(string message, string title = "Confirm");
        void ShowWarning(string message, string title = "Warning");
        void ShowError(string message, string title = "Error");
    }
}