using System;
using System.Linq;

namespace NINA.Plugin.SeeDither {
    /// <summary>
    /// A Seestar model paired with its imaging plate scale (arcsec per pixel).
    /// The selected scope drives the plate scale used only for the ~px pixel
    /// estimates shown in the settings UI. Actual dithering logic uses
    /// arcseconds only and never consults this.
    /// </summary>
    public class SeestarScope {
        public string DisplayName { get; }
        public double PlateScaleArcSecPerPx { get; }

        public SeestarScope(string displayName, double plateScaleArcSecPerPx) {
            DisplayName = displayName;
            PlateScaleArcSecPerPx = plateScaleArcSecPerPx;
        }

        public static readonly SeestarScope[] All = {
            new SeestarScope("Seestar S30", 3.99),
            new SeestarScope("Seestar S30 Pro", 3.74),
            new SeestarScope("Seestar S50", 2.39),
            new SeestarScope("Seestar S50 Pro", 2.30)
        };

        public static SeestarScope Default => All[0];

        public static SeestarScope FromName(string name) {
            return All.FirstOrDefault(s => string.Equals(s.DisplayName, name, StringComparison.OrdinalIgnoreCase)) ?? Default;
        }
    }
}