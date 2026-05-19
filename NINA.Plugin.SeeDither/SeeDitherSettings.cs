using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json;
using NINA.Core.Utility;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither {
    [Serializable]
    public class SeeDitherSettings : INotifyPropertyChanged {
        private bool _enabled = true;
        private int _exposuresBetween = 2;
        private double _minOffsetArcsec = 5.0;
        private double _maxOffsetArcsec = 60.0;
        private double _plateScaleArcSecPerPx = 2.39;
        private double _slewSettleSeconds = 2.0;

        [field: NonSerialized]
        private bool _suspendSave;

        public SeeDitherSettings() { }

        public bool Enabled {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); Save(); } }
        }

        public int ExposuresBetween {
            get => _exposuresBetween;
            set {
                var clamped = Math.Max(1, Math.Min(100, value));
                if (_exposuresBetween != clamped) {
                    _exposuresBetween = clamped;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public double MinOffsetArcsec {
            get => _minOffsetArcsec;
            set {
                var clamped = Math.Max(0.1, Math.Min(3600.0, value));
                if (clamped >= MaxOffsetArcsec) clamped = MaxOffsetArcsec - 0.1;
                if (_minOffsetArcsec != clamped) {
                    _minOffsetArcsec = clamped;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public double MaxOffsetArcsec {
            get => _maxOffsetArcsec;
            set {
                var clamped = Math.Max(0.2, Math.Min(3600.0, value));
                if (clamped <= MinOffsetArcsec) clamped = MinOffsetArcsec + 0.1;
                if (_maxOffsetArcsec != clamped) {
                    _maxOffsetArcsec = clamped;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public double PlateScaleArcSecPerPx {
            get => _plateScaleArcSecPerPx;
            set {
                var clamped = Math.Max(0.01, Math.Min(100.0, value));
                if (_plateScaleArcSecPerPx != clamped) {
                    _plateScaleArcSecPerPx = clamped;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public double SlewSettleSeconds {
            get => _slewSettleSeconds;
            set {
                var clamped = Math.Max(0.0, Math.Min(30.0, value));
                if (_slewSettleSeconds != clamped) {
                    _slewSettleSeconds = clamped;
                    OnPropertyChanged();
                    Save();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static readonly string SettingsPath = Path.Combine(NINA.Core.Utility.CoreUtil.APPLICATIONTEMPPATH, "SeeDither", "settings.json");

        public static SeeDitherSettings Load() {
            try {
                if (File.Exists(SettingsPath)) {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<SeeDitherSettings>(json);
                    if (settings != null) return settings;
                }
                var defaults = new SeeDitherSettings();
                defaults.Save();
                return defaults;
            } catch (Exception ex) {
                SeeDitherLog.Error("Failed to load settings", ex);
                var defaults = new SeeDitherSettings();
                defaults.Save();
                return defaults;
            }
        }

        public void Save() {
            if (_suspendSave) return;
            try {
                string dir = Path.GetDirectoryName(SettingsPath);
                Directory.CreateDirectory(dir);
                string tmpPath = SettingsPath + ".tmp";
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(tmpPath, json);
                if (File.Exists(SettingsPath))
                    File.Delete(SettingsPath);
                File.Move(tmpPath, SettingsPath);
            } catch (Exception ex) {
                SeeDitherLog.Error("Failed to save settings", ex);
            }
        }

        internal void SuspendSave() { _suspendSave = true; }
    }
}
