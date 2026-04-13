using FileProcessing;
using FileProcessing.Abstractions;
using FileProcessing.Importers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Visualization.Interaction;
using Visualization.Scene;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Camera;
using Visualization.UI.ViewModels;
using Visualization.UI.Windows;

namespace Shell
{
    public sealed partial class App
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            services.AddSingleton<ICameraSystem, CameraSystem>();
            services.AddSingleton<ISceneRenderer, SceneRenderer>();
            services.AddSingleton<VisualizationEngine>();

            services.AddSingleton<IMeshImporter, ObjMeshImporter>();
            services.AddSingleton<IMeshImporter, StlMeshImporter>();
            services.AddSingleton<IMeshImporterFactory, MeshImporterFactory>();

            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            base.OnExit(e);
        }
    }
}
