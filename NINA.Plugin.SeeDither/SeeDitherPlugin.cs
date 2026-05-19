using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;

namespace NINA.Plugin.SeeDither {
    [Export(typeof(IPluginManifest))]
    public class SeeDitherPlugin : PluginBase {
        private static SeeDitherSettings _settings;
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
                var rd = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(rd);

                CameraMediator.Connected += OnCameraConnected;
            } catch (Exception ex) {
                // Plugin resources failed to load — NINA will still work without UI
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
            } catch {
                return 3.74;
            }
        }
    }
}
