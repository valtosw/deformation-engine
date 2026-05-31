using System.Windows;
using Application.Core.Abstractions;
using Application.UI.Views;

namespace Application.UI.Services
{
    public sealed class DialogService : IDialogService
    {
        #region Public Logic

        public bool ShowConfirmation(string message, string title = "Confirm")
        {
            return ShowMessageBox(message, title, MessageBoxButton.YesNo);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            ShowMessageBox(message, title, MessageBoxButton.OK);
        }

        public void ShowError(string message, string title = "Error")
        {
            ShowMessageBox(message, title, MessageBoxButton.OK);
        }

        #endregion

        #region Private Logic

        private static bool ShowMessageBox(string message, string title, MessageBoxButton messageBoxButton)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;

            if (mainWindow is null)
            {
                return false;
            }

            return MessageBoxWindow.Show(mainWindow, message, title, messageBoxButton);
        }

        #endregion
    }
}