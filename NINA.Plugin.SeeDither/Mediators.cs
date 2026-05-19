using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;

namespace NINA.Plugin.SeeDither {
    internal static class Mediators {
        public static IProfileService ProfileService { get; set; }
        public static ITelescopeMediator TelescopeMediator { get; set; }
    }
}
