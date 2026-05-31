using Application.Core.Abstractions;
using Application.UI.Views;

namespace Application.UI.Services
{
    public sealed class DialogService : IDialogService
    {
        #region Public Logic

        public bool ShowConfirmation(string message, string title = "Confirm")
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;

            if (mainWindow is null)
            {
                return false;
            }

            return MessageBoxWindow.Show(mainWindow, message, title);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;

            if (mainWindow is not null)
            {
                MessageBoxWindow.Show(mainWindow, message, title);
            }
        }

        public void ShowError(string message, string title = "Error")
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;

            if (mainWindow is not null)
            {
                MessageBoxWindow.Show(mainWindow, message, title);
            }
        }

        #endregion
    }
}