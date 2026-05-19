using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model.Application;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;

namespace NINA.Plugin.SeeDither.Sequencer.Triggers {
    [ExportMetadata("Name", "SeeDither After Exposures")]
    [ExportMetadata("Description", "Dithers via absolute GoTo offsets; designed for Seestar mounts.")]
    [ExportMetadata("Icon", "DitherSVG")]
    [ExportMetadata("Category", "Telescope")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SeeDitherAfterExposuresTrigger : SequenceTrigger {
        private readonly Random _rng = new Random();
        private int _exposureCounter = 0;
        private Coordinates _baseCoords = null;
        private readonly object _stateLock = new object();

        [JsonProperty]
        public bool Enabled {
            get => SeeDitherPlugin.Settings?.Enabled ?? true;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.Enabled = value; OnPropertyChanged(nameof(Enabled)); } }
        }

        [JsonProperty]
        public int ExposuresBetween {
            get => SeeDitherPlugin.Settings?.ExposuresBetween ?? 2;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.ExposuresBetween = value; OnPropertyChanged(nameof(ExposuresBetween)); } }
        }

        [JsonProperty]
        public double MinOffsetArcsec {
            get => SeeDitherPlugin.Settings?.MinOffsetArcsec ?? 5.0;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.MinOffsetArcsec = value; OnPropertyChanged(nameof(MinOffsetArcsec)); } }
        }

        [JsonProperty]
        public double MaxOffsetArcsec {
            get => SeeDitherPlugin.Settings?.MaxOffsetArcsec ?? 60.0;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.MaxOffsetArcsec = value; OnPropertyChanged(nameof(MaxOffsetArcsec)); } }
        }

        [JsonProperty]
        public double PlateScaleArcSecPerPx {
            get => SeeDitherPlugin.Settings?.PlateScaleArcSecPerPx ?? 2.39;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.PlateScaleArcSecPerPx = value; OnPropertyChanged(nameof(PlateScaleArcSecPerPx)); } }
        }

        [JsonProperty]
        public double SlewSettleSeconds {
            get => SeeDitherPlugin.Settings?.SlewSettleSeconds ?? 2.0;
            set { if (SeeDitherPlugin.Settings != null) { SeeDitherPlugin.Settings.SlewSettleSeconds = value; OnPropertyChanged(nameof(SlewSettleSeconds)); } }
        }

        [ImportingConstructor]
        public SeeDitherAfterExposuresTrigger() : base() { }

        private SeeDitherAfterExposuresTrigger(SeeDitherAfterExposuresTrigger copyMe) : this() { }

        public override object Clone() => new SeeDitherAfterExposuresTrigger(this) { Icon = Icon, Name = Name, Category = Category, Description = Description };

        public override void Initialize() {
            lock (_stateLock) {
                _exposureCounter = 0;
                _baseCoords = null;
            }
            SeeDitherLog.Info("Trigger initialized.");
        }

        public override void Teardown() {
            lock (_stateLock) {
                _baseCoords = null;
                _exposureCounter = 0;
            }
            SeeDitherLog.Info("Trigger torn down.");
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            try {
                if (!Enabled) return false;
                if (previousItem == null) return false;

                string typeName = previousItem.GetType().Name;
                bool isExposure = previousItem is IExposureItem || typeName.Contains("TakeExposure");
                if (!isExposure) return false;

                lock (_stateLock) {
                    _exposureCounter++;
                    int interval = Math.Max(1, ExposuresBetween);
                    return _exposureCounter % interval == 0;
                }
            } catch (Exception ex) {
                SeeDitherLog.Error("ShouldTrigger failed", ex);
                return false;
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                token.ThrowIfCancellationRequested();

                var telescope = Mediators.TelescopeMediator;
                if (telescope == null) {
                    SeeDitherLog.Error("Telescope mediator is null.");
                    return;
                }

                var info = telescope.GetInfo();
                if (info == null || !info.Connected) {
                    SeeDitherLog.Warn("Telescope not connected; skipping dither.");
                    return;
                }

                lock (_stateLock) {
                    if (_baseCoords == null && context != null) {
                        var parent = context as ISequenceItem;
                        Coordinates found = null;

                        while (parent != null) {
                            var targetType = parent.GetType();
                            var targetProp = targetType.GetProperty("Target");
                            if (targetProp != null) {
                                var target = targetProp.GetValue(parent);
                                if (target != null) {
                                    var coordsProp = target.GetType().GetProperty("Coordinates") ?? target.GetType().GetProperty("InputCoordinates");
                                    if (coordsProp != null) {
                                        found = coordsProp.GetValue(target) as Coordinates;
                                        break;
                                    }
                                }
                            }
                            parent = parent.Parent;
                        }

                        if (found != null) {
                            _baseCoords = new Coordinates(found.RA, found.Dec, found.Epoch);
                        } else {
                            _baseCoords = new Coordinates(Angle.ByHours(info.RightAscension), Angle.ByDegree(info.Declination), Epoch.JNOW);
                            SeeDitherLog.Warn("Base coordinates captured from telescope, not target.");
                        }
                    }

                    if (_baseCoords == null) {
                        _baseCoords = new Coordinates(Angle.ByHours(info.RightAscension), Angle.ByDegree(info.Declination), Epoch.JNOW);
                    }
                }

                if (MinOffsetArcsec >= MaxOffsetArcsec) {
                    SeeDitherLog.Error("Invalid offset range: Min >= Max.");
                    return;
                }

                var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(_rng, MinOffsetArcsec, MaxOffsetArcsec);

                Coordinates target;
                lock (_stateLock) {
                    target = AstrometryOffset.ApplyOffset(_baseCoords, raArc, decArc);
                }

                SeeDitherLog.Info($"Dithering by RA={raArc:F1}\" Dec={decArc:F1}\" → RA={target.RAString} Dec={target.DecString}");

                progress?.Report(new ApplicationStatus { Status = "SeeDither: slewing offset" });

                bool ok = false;
                try {
                    ok = await telescope.SlewToCoordinatesAsync(target, token);
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    SeeDitherLog.Error("SlewToCoordinatesAsync exception", ex);
                }

                if (!ok) {
                    SeeDitherLog.Warn("SlewToCoordinatesAsync returned false.");
                }

                if (SlewSettleSeconds > 0) {
                    await Task.Delay(TimeSpan.FromSeconds(SlewSettleSeconds), token);
                }

                progress?.Report(new ApplicationStatus { Status = string.Empty });
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                SeeDitherLog.Error("Execute failed", ex);
            }
        }

        public override string ToString() => $"Category: {Category}, Item: nameof(SeeDitherAfterExposuresTrigger), Enabled: {Enabled}, Every: {ExposuresBetween}, Range: [{MinOffsetArcsec},{MaxOffsetArcsec}] arcsec";

        public override bool Validate() {
            Issues.Clear();

            if (Mediators.TelescopeMediator == null) {
                Issues.Add("Telescope mediator is unavailable.");
            } else {
                var info = Mediators.TelescopeMediator?.GetInfo();
                if (info == null || !info.Connected) {
                    Issues.Add("Telescope not connected.");
                }
            }

            if (!(MinOffsetArcsec < MaxOffsetArcsec)) {
                Issues.Add("MinOffset must be less than MaxOffset.");
            }

            if (ExposuresBetween < 1) {
                Issues.Add("ExposuresBetween must be >= 1.");
            }

            return Issues.Count == 0;
        }
    }
}
