using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither {
    [Export(typeof(IPluginManifest))]
    public class SeeDitherPlugin : PluginBase, IDisposable {
        private static SeeDitherSettings _settings;
        private static ResourceDictionary _resourceDictionary;
        private bool _disposed;

        public static SeeDitherSettings Settings => _settings;
        public SeeDitherSettings SettingsInstance { get; }
        public ICameraMediator CameraMediator { get; }

        [ImportingConstructor]
        public SeeDitherPlugin(ICameraMediator cameraMediator) {
            try {
                CameraMediator = cameraMediator;
                _settings = SeeDitherSettings.Load();
                _settings.LoadPlateScaleFromCamera(GetPlateScaleFromCamera);
                SettingsInstance = _settings;
                var uri = new Uri("pack://application:,,,/NINA.Plugin.SeeDither;component/Resources.xaml");
                _resourceDictionary = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(_resourceDictionary);

                CameraMediator.Connected += OnCameraConnected;
            } catch (Exception ex) {
                SeeDitherLog.Error("Plugin initialization failed", ex);
            }
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            try {
                CameraMediator.Connected -= OnCameraConnected;
                if (_resourceDictionary != null) {
                    Application.Current.Resources.MergedDictionaries.Remove(_resourceDictionary);
                }
            } catch (Exception ex) {
                SeeDitherLog.Error("Plugin dispose failed", ex);
            }
        }

        public override string ToString() => "SeeDither";

        private async Task OnCameraConnected(object sender, EventArgs e) {
            await Task.Run(() => {
                _settings.LoadPlateScaleFromCamera(GetPlateScaleFromCamera);
            });
        }

        private double GetPlateScaleFromCamera() {
            try {
                var info = CameraMediator?.GetInfo();
                if (info == null || !info.Connected) return 3.74;
                var name = info.Name ?? "";
                var upper = name.ToUpperInvariant();
                if (upper.Contains("S50")) return 2.39;
                if (upper.Contains("S30")) return 3.74;
                return 3.74;
            } catch (Exception ex) {
                SeeDitherLog.Error("GetPlateScaleFromCamera failed", ex);
                return 3.74;
            }
        }
    }
}
