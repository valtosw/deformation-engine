using System.Windows;

namespace Application.UI.Views
{
    public sealed partial class MessageBoxWindow
    {
        #region Constructors

        public MessageBoxWindow(string message, string title)
        {
            InitializeComponent();
            MessageText.Text = message;
            Title = title;
        }

        #endregion

        #region Properties

        public bool Result { get; private set; }

        #endregion

        #region Public Logic

        public static bool Show(Window owner, string message, string title)
        {
            var window = new MessageBoxWindow(message, title)
            {
                Owner = owner
            };

            window.ShowDialog();

            return window.Result;
        }

        #endregion

        #region Private Logic

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
