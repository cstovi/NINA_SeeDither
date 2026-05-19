using System.ComponentModel.Composition;
using NINA.Plugin;
using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither {
    [Export(typeof(IPluginManifest))]
    public class SeeDitherPlugin : PluginBase {
        private readonly IProfileService _profileService;
        private readonly ITelescopeMediator _telescopeMediator;

        public static SeeDitherSettings Settings { get; private set; }

        [ImportingConstructor]
        public SeeDitherPlugin(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            try {
                Mediators.ProfileService = profileService;
                Mediators.TelescopeMediator = telescopeMediator;
                Settings = SeeDitherSettings.Load();
            } catch (Exception ex) {
                SeeDitherLog.Error("SeeDitherPlugin constructor failed", ex);
            }
        }

        public override string ToString() => "SeeDither";
    }
}
