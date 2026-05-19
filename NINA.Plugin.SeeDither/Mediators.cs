using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.Plugin.SeeDither {
    internal static class Mediators {
        public static IProfileService ProfileService { get; set; }
        public static ITelescopeMediator TelescopeMediator { get; set; }
    }
}
