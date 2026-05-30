using Application.UI.ViewModels;
using Application.UI.Windows;
using Deformation.Interaction;
using Deformation.IO;
using Deformation.IO.Abstractions;
using Deformation.IO.Importers;
using Deformation.Scene;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Camera;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Application.Host
{
    public sealed partial class App
    {
        private IServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs eventArguments)
        {
            try
            {
                base.OnStartup(eventArguments);

                var services = new ServiceCollection();

                services.AddSingleton<ICameraSystem, CameraSystem>();
                services.AddSingleton<IGizmoSystem, GizmoSystem>();
                services.AddSingleton<ISceneRenderer, SceneRenderer>();
                services.AddSingleton<ControllerEngine>();

                services.AddSingleton<IMeshImporter, ObjMeshImporter>();
                services.AddSingleton<IMeshImporter, StlMeshImporter>();
                services.AddSingleton<IMeshImporter, AssimpMeshImporter>();
                services.AddSingleton<IMeshImporter, GltfMeshImporter>();
                services.AddSingleton<IMeshImporterFactory, MeshImporterFactory>();

                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();

                _serviceProvider = services.BuildServiceProvider();

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Application failed to start.\n\nError: {exception.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs eventArguments)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(eventArguments);
        }
    }
}
