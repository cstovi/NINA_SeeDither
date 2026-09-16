using System;
using System.ComponentModel.Composition;
using System.Windows;
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

        public SeeDitherPlugin() {
            try {
                _settings = SeeDitherSettings.Load();
                SettingsInstance = _settings;
                var uri = new Uri("pack://application:,,,/NINA.Plugin.SeeDither;component/Resources.xaml");
                _resourceDictionary = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(_resourceDictionary);
            } catch (Exception ex) {
                SeeDitherLog.Error("Plugin initialization failed", ex);
            }
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;
            try {
                if (_resourceDictionary != null) {
                    Application.Current.Resources.MergedDictionaries.Remove(_resourceDictionary);
                }
            } catch (Exception ex) {
                SeeDitherLog.Error("Plugin dispose failed", ex);
            }
        }

        public override string ToString() => "SeeDither";
    }
}
