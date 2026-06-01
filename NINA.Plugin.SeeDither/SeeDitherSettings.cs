using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NINA.Core.Utility;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither {
    [Serializable]
    public class SeeDitherSettings : INotifyPropertyChanged {
        private bool _enabled = true;
        private int _exposuresBetween = 2;
        private int _minOffsetArcsec = 20;
        private int _maxOffsetArcsec = 150;
        private double _plateScaleArcSecPerPx = 3.99;
        private string _minOffsetArcsecText = "20";
        private string _maxOffsetArcsecText = "150";

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

        public int MinOffsetArcsec {
            get => _minOffsetArcsec;
            internal set {
                var clamped = Math.Max(1, Math.Min(500, value));
                if (_minOffsetArcsec != clamped) {
                    _minOffsetArcsec = clamped;
                    // Auto-raise Max if Min exceeds it — allows Min == Max
                    if (!_suspendSave && clamped > _maxOffsetArcsec) {
                        _maxOffsetArcsec = clamped;
                        OnPropertyChanged(nameof(MaxOffsetArcsec));
                        OnPropertyChanged(nameof(MaxOffsetPixels));
                        _maxOffsetArcsecText = clamped.ToString();
                        OnPropertyChanged(nameof(MaxOffsetArcsecText));
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MinOffsetPixels));
                    Save();
                }
            }
        }

        public string MinOffsetArcsecText {
            get => _minOffsetArcsecText;
            set {
                if (_minOffsetArcsecText != value) {
                    _minOffsetArcsecText = value;
                    OnPropertyChanged();
                    if (_suspendSave) return;
                    if (int.TryParse(value, out var parsed)) {
                        if (parsed < 1 || parsed > 500) {
                            throw new ArgumentOutOfRangeException(nameof(value), "Min offset must be between 1 and 500 arcseconds.");
                        }
                        MinOffsetArcsec = parsed;
                        _minOffsetArcsecText = _minOffsetArcsec.ToString();
                        OnPropertyChanged(nameof(MinOffsetArcsecText));
                    } else if (!string.IsNullOrWhiteSpace(value)) {
                        throw new FormatException("Min offset must be a whole number.");
                    }
                }
            }
        }

        public int MaxOffsetArcsec {
            get => _maxOffsetArcsec;
            internal set {
                var clamped = Math.Max(1, Math.Min(500, value));
                if (_maxOffsetArcsec != clamped) {
                    _maxOffsetArcsec = clamped;
                    // Auto-drop Min if Max falls below it — allows Min == Max
                    if (!_suspendSave && clamped < _minOffsetArcsec) {
                        _minOffsetArcsec = clamped;
                        OnPropertyChanged(nameof(MinOffsetArcsec));
                        OnPropertyChanged(nameof(MinOffsetPixels));
                        _minOffsetArcsecText = clamped.ToString();
                        OnPropertyChanged(nameof(MinOffsetArcsecText));
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MaxOffsetPixels));
                    Save();
                }
            }
        }

        public string MaxOffsetArcsecText {
            get => _maxOffsetArcsecText;
            set {
                if (_maxOffsetArcsecText != value) {
                    _maxOffsetArcsecText = value;
                    OnPropertyChanged();
                    if (_suspendSave) return;
                    if (int.TryParse(value, out var parsed)) {
                        if (parsed < 1 || parsed > 500) {
                            throw new ArgumentOutOfRangeException(nameof(value), "Max offset must be between 1 and 500 arcseconds.");
                        }
                        MaxOffsetArcsec = parsed;
                        _maxOffsetArcsecText = _maxOffsetArcsec.ToString();
                        OnPropertyChanged(nameof(MaxOffsetArcsecText));
                    } else if (!string.IsNullOrWhiteSpace(value)) {
                        throw new FormatException("Max offset must be a whole number.");
                    }
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
                    OnPropertyChanged(nameof(MinOffsetPixels));
                    OnPropertyChanged(nameof(MaxOffsetPixels));
                    Save();
                }
            }
        }

        public double MinOffsetPixels => MinOffsetArcsec / PlateScaleArcSecPerPx;

        public double MaxOffsetPixels => MaxOffsetArcsec / PlateScaleArcSecPerPx;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private static readonly string SettingsPath = Path.Combine(NINA.Core.Utility.CoreUtil.APPLICATIONTEMPPATH, "SeeDither", "settings.json");
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver(),
            Converters = { new StringEnumConverter() }
        };

        public static SeeDitherSettings Load() {
            try {
                if (File.Exists(SettingsPath)) {
                    var json = File.ReadAllText(SettingsPath);
                    var dto = JsonConvert.DeserializeObject<SettingsDto>(json);
                    if (dto != null) {
                        var settings = new SeeDitherSettings();
                        settings._enabled = dto.Enabled;
                        settings._exposuresBetween = Math.Max(1, Math.Min(100, dto.ExposuresBetween));
                        settings._minOffsetArcsec = Math.Max(1, Math.Min(500, dto.MinOffsetArcsec));
                        settings._maxOffsetArcsec = Math.Max(1, Math.Min(500, dto.MaxOffsetArcsec));
                        if (settings._minOffsetArcsec > settings._maxOffsetArcsec) {
                            settings._minOffsetArcsec = settings._maxOffsetArcsec;
                        }
                        settings._plateScaleArcSecPerPx = Math.Max(0.01, Math.Min(100.0, dto.PlateScaleArcSecPerPx));
                        settings._minOffsetArcsecText = settings._minOffsetArcsec.ToString();
                        settings._maxOffsetArcsecText = settings._maxOffsetArcsec.ToString();
                        return settings;
                    }
                }
                var defaults = new SeeDitherSettings();
                defaults.SuspendSave();
                defaults._minOffsetArcsecText = defaults._minOffsetArcsec.ToString();
                defaults._maxOffsetArcsecText = defaults._maxOffsetArcsec.ToString();
                defaults.Save();
                defaults.ResumeSave();
                return defaults;
            } catch (Exception ex) {
                SeeDitherLog.Error("Failed to load settings", ex);
                var defaults = new SeeDitherSettings();
                defaults.SuspendSave();
                defaults._minOffsetArcsecText = defaults._minOffsetArcsec.ToString();
                defaults._maxOffsetArcsecText = defaults._maxOffsetArcsec.ToString();
                defaults.Save();
                defaults.ResumeSave();
                return defaults;
            }
        }

        // Bare DTO for deserialization — no validation, no side effects.
        private class SettingsDto {
            public bool Enabled { get; set; } = true;
            public int ExposuresBetween { get; set; } = 2;
            public int MinOffsetArcsec { get; set; } = 20;
            public int MaxOffsetArcsec { get; set; } = 150;
            public double PlateScaleArcSecPerPx { get; set; } = 3.99;
        }

        public void Save() {
            if (_suspendSave) return;
            try {
                string dir = Path.GetDirectoryName(SettingsPath);
                Directory.CreateDirectory(dir);
                string tmpPath = SettingsPath + ".tmp";
                string json = JsonConvert.SerializeObject(this, JsonSettings);
                File.WriteAllText(tmpPath, json);
                if (File.Exists(SettingsPath))
                    File.Delete(SettingsPath);
                File.Move(tmpPath, SettingsPath);
            } catch (Exception ex) {
                SeeDitherLog.Error("Failed to save settings", ex);
            }
        }

        public void SuspendSave() => _suspendSave = true;

        public void ResumeSave() => _suspendSave = false;

        public void LoadPlateScaleFromCamera(Func<double> getPlateScale) {
            try {
                var detected = getPlateScale();
                if (detected > 0.01 && detected < 100.0) {
                    _plateScaleArcSecPerPx = detected;
                    OnPropertyChanged(nameof(MinOffsetPixels));
                    OnPropertyChanged(nameof(MaxOffsetPixels));
                }
            } catch (Exception ex) {
                SeeDitherLog.Error("LoadPlateScaleFromCamera failed", ex);
            }
        }
    }
}
