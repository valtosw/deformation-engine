using Logging;
using Microsoft.Extensions.Logging;
using Visualization.UI;
using Visualization.UI.Windows;

namespace Shell
{
    internal sealed class Program
    {
        [STAThread]
        private static void Main()
        {
            var logger = Log.Create<Program>();

            try
            {
                var application = new App();
                var window = new MainWindow();

                application.Run(window);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred.");
            }
        }
    }
}
