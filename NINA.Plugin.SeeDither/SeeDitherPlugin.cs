using System;
using System.ComponentModel.Composition;
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

        [ImportingConstructor]
        public SeeDitherPlugin() {
            try {
                _settings = SeeDitherSettings.Load();
                SettingsInstance = _settings;
                var uri = new Uri("pack://application:,,,/NINA.Plugin.SeeDither;component/Resources.xaml");
                var rd = new ResourceDictionary { Source = uri };
                Application.Current.Resources.MergedDictionaries.Add(rd);
            } catch (Exception ex) {
                // Plugin resources failed to load — NINA will still work without UI
            }
        }

        public override string ToString() => "SeeDither";
    }
}
