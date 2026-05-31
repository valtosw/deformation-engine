using System.Windows;

namespace Application.UI.Views
{
    public sealed partial class MessageBoxWindow
    {
        #region Constructors

        public MessageBoxWindow(string message, string title, MessageBoxButton buttons)
        {
            InitializeComponent();
            MessageText.Text = message;
            Title = title;

            if (buttons == MessageBoxButton.OK)
            {
                OkButton.Visibility = Visibility.Visible;
            }
            else if (buttons == MessageBoxButton.YesNo)
            {
                YesButton.Visibility = Visibility.Visible;
                NoButton.Visibility = Visibility.Visible;
            }
        }

        #endregion

        #region Properties

        public bool Result { get; private set; }

        #endregion

        #region Public Logic

        public static bool Show(Window owner, string message, string title, MessageBoxButton buttons)
        {
            var window = new MessageBoxWindow(message, title, buttons)
            {
                Owner = owner
            };

            window.ShowDialog();

            return window.Result;
        }

        #endregion

        #region Private Logic

        private void Ok_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void Yes_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void No_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        private void Close_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        #endregion
    }
}