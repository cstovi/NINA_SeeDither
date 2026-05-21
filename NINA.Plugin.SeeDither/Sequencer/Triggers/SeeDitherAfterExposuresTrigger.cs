using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.SeeDither.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;

namespace NINA.Plugin.SeeDither.Sequencer.Triggers {
    [Export]
    [ExportMetadata("Name", "SeeDither After Exposures")]
    [ExportMetadata("Description", "Dithers via absolute GoTo offsets; designed for Seestar mounts.")]
    [ExportMetadata("Icon", "DitherSVG")]
    [ExportMetadata("Category", "SeeDither")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class SeeDitherAfterExposuresTrigger : SequenceTrigger {
        private readonly ITelescopeMediator _telescopeMediator;
        private int _exposureCounter = 0;
        private readonly object _stateLock = new object();

        private int _exposuresBetween = 2;
        private int _currentCount = 0;

        [JsonProperty]
        public int ExposuresBetween {
            get => _exposuresBetween;
            set {
                if (_exposuresBetween != value) {
                    _exposuresBetween = Math.Max(1, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public int CurrentCount {
            get => _currentCount;
            private set {
                if (_currentCount != value) {
                    _currentCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public string ProgressText => $"{CurrentCount}/{ExposuresBetween}";

        [ImportingConstructor]
        public SeeDitherAfterExposuresTrigger(ITelescopeMediator telescopeMediator) : base() {
            _telescopeMediator = telescopeMediator;
        }

        private SeeDitherAfterExposuresTrigger(SeeDitherAfterExposuresTrigger copyMe) : this(copyMe._telescopeMediator) {
            _exposuresBetween = copyMe._exposuresBetween;
        }

        public override object Clone() => new SeeDitherAfterExposuresTrigger(this) { Icon = Icon, Name = Name, Category = Category, Description = Description };

        public override void Initialize() {
            lock (_stateLock) {
                _exposureCounter = 0;
                _currentCount = 0;
            }
            SeeDitherLog.Info("Trigger initialized.");
        }

        public override void Teardown() {
            lock (_stateLock) {
                _exposureCounter = 0;
                _currentCount = 0;
            }
            SeeDitherLog.Info("Trigger torn down.");
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            try {
                if (previousItem == null) return false;

                if (previousItem is not IExposureItem) return false;

                bool shouldFire;
                lock (_stateLock) {
                    _exposureCounter++;
                    int interval = Math.Max(1, ExposuresBetween);
                    shouldFire = (_exposureCounter % interval == 0);
                    _currentCount = shouldFire ? 0 : _exposureCounter % interval;
                    OnPropertyChanged(nameof(CurrentCount));
                    OnPropertyChanged(nameof(ProgressText));
                }
                return shouldFire;
            } catch (Exception ex) {
                SeeDitherLog.Error("ShouldTrigger failed", ex);
                return false;
            }
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                token.ThrowIfCancellationRequested();

                if (_telescopeMediator == null) {
                    SeeDitherLog.Error("Telescope mediator is null.");
                    return;
                }

                var info = _telescopeMediator.GetInfo();
                if (info == null || !info.Connected) {
                    SeeDitherLog.Warn("Telescope not connected; skipping dither.");
                    return;
                }

                var baseCoords = _telescopeMediator.GetCurrentPosition();
                if (baseCoords == null) {
                    SeeDitherLog.Error("GetCurrentPosition returned null.");
                    return;
                }
                SeeDitherLog.Info($"Base coordinates: RA={baseCoords.RAString} Dec={baseCoords.DecString}");

                var settings = SeeDitherPlugin.Settings;
                int minOffset = settings?.MinOffsetArcsec ?? 5;
                int maxOffset = settings?.MaxOffsetArcsec ?? 60;

                if (minOffset > maxOffset) {
                    SeeDitherLog.Error("Invalid offset range: Min > Max.");
                    return;
                }

                var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(Random.Shared, minOffset, maxOffset);

                Coordinates target = AstrometryOffset.ApplyOffset(baseCoords, raArc, decArc);

                SeeDitherLog.Info($"Dithering by RA={raArc:F1}\" Dec={decArc:F1}\" → RA={target.RAString} Dec={target.DecString}");

                progress?.Report(new ApplicationStatus { Status = "SeeDither: slewing offset" });

                SeeDitherLog.Info($"CanSlew={info.CanSlew}, TrackingEnabled={info.TrackingEnabled}, AtPark={info.AtPark}");

                bool ok = false;
                try {
                    ok = await _telescopeMediator.SlewToCoordinatesAsync(target, token);
                    SeeDitherLog.Info($"SlewToCoordinatesAsync returned: {ok}");
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    SeeDitherLog.Error("SlewToCoordinatesAsync exception", ex);
                }

                if (!ok) {
                    SeeDitherLog.Warn("SlewToCoordinatesAsync returned false.");
                }

                var newPos = _telescopeMediator.GetCurrentPosition();
                SeeDitherLog.Info($"Post-slew position: RA={newPos?.RAString} Dec={newPos?.DecString}");

                progress?.Report(new ApplicationStatus { Status = string.Empty });
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                SeeDitherLog.Error("Execute failed", ex);
            }
        }

        public override string ToString() => $"Category: {Category}, Item: SeeDitherAfterExposuresTrigger, Every: {ExposuresBetween}";
    }
}
