using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("NINA.Plugin.SeeDither.Tests")]

[assembly: AssemblyTitle("SeeDither")]
[assembly: AssemblyDescription("Absolute GoTo coordinate-offset dithering for Seestar S30/S50 mounts.")]
[assembly: AssemblyCompany("Carl Stovell")]
[assembly: AssemblyProduct("SeeDither")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: ComVisible(false)]
[assembly: Guid("c4d2e5a1-7b3f-4e9d-9f2a-6f1c8b9a3d11")]
[assembly: AssemblyVersion("1.6.0.0")]
[assembly: AssemblyFileVersion("1.6.0.0")]
[assembly: AssemblyInformationalVersion("1.6.0.0")]

[assembly: AssemblyMetadata("Id", "c4d2e5a1-7b3f-4e9d-9f2a-6f1c8b9a3d11")]
[assembly: AssemblyMetadata("Name", "SeeDither")]
[assembly: AssemblyMetadata("Author", "Carl S.")]
[assembly: AssemblyMetadata("Homepage", "https://ko-fi.com/turnpike47298")]
[assembly: AssemblyMetadata("Repository", "")]
[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/cstovi/NINA_SeeDither/releases")]
[assembly: AssemblyMetadata("FeaturedImageURL", "https://i.ibb.co/v6C6vXjp/seedither.jpg")]
[assembly: AssemblyMetadata("ScreenshotURL", "")]
[assembly: AssemblyMetadata("AltScreenshotURL", "")]
[assembly: AssemblyMetadata("LongDescription", "Performs random absolute-coordinate dithering after exposures, designed for Seestar mounts whose guide pulses are unreliable.\n\nHopefully ZWO can address what some of us have found appear to be dithering issues (e.g. not using both RA and DEC axes effectively). If you experience dithering problems in the Seestar app or in the NINA Seestar ASCOM Alpaca setup, it's always best to report them so ZWO is aware.\n\nThis plugin presents a basic, alternative way to get some RA and DEC dithering for Seestars, and is hopefully a temporary measure until ZWO can address it long-term.\n\nIf you use and like anything I've done, support on Ko-fi (https://ko-fi.com/turnpike47298) is appreciated to encourage me to keep going!")]
