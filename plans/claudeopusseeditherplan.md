

# Execution Blueprint: SeeDither NINA Plugin

## Critique Summary (issues fixed below)

- **Contradiction**: Plan says "fire-and-forget" then "poll until Idle". Resolved: use `ITelescopeMediator.SlewToCoordinatesAsync` which awaits completion internally.
- **Drift accumulation**: Plan says "current target RA/Dec + offsets" — if always read from telescope, errors compound. Fixed: capture **base target coordinates** once per DSO container and offset from base.
- **Wrong RA math**: "time-seconds for GoTo" is incorrect. NINA `Coordinates` stores RA in hours. Converted properly: `ΔRA_hours = (Δarcsec / 3600 / 15) / cos(Dec)`.
- **Missing**: cancellation handling, telescope-not-connected guards, validation (min<max), thread-safe RNG, IProgress reporting, exposure-counter persistence across `ShouldTrigger`/`Execute`, MEF imports for mediators.
- **Interface**: NINA uses `SequenceTrigger` base + `ISequenceTrigger`; there is no separate `IAfterExposuresTrigger`. "After Exposures" semantics are achieved via `ShouldTrigger(previousItem, nextItem)` returning true when `previousItem is IExposureItem`.
- **Settings path**: must use `NINA.Core.Utility.CoreUtil` profile-based path, not arbitrary file.
- **Trigger lifecycle**: must implement `Clone()`, `Initialize()`, `Teardown()`, `ToString()`.

---

## STRICTLY SEQUENTIAL EXECUTION STEPS

### STEP 1 — File: `NINA_SeeDither/NINA.Plugin.SeeDither.sln`
1.1. Create a Visual Studio 2022 solution file targeting a single project `NINA.Plugin.SeeDither.csproj` (path: `NINA.Plugin.SeeDither/NINA.Plugin.SeeDither.csproj`). Configuration: `Debug|AnyCPU` and `Release|AnyCPU`.

### STEP 2 — File: `NINA.Plugin.SeeDither/NINA.Plugin.SeeDither.csproj`
2.1. Use SDK `Microsoft.NET.Sdk`.
2.2. Set `<TargetFramework>net8.0-windows</TargetFramework>`.
2.3. Set `<UseWPF>true</UseWPF>`, `<UseWindowsForms>false</UseWindowsForms>`, `<LangVersion>latest</LangVersion>`, `<Nullable>disable</Nullable>`, `<Platforms>AnyCPU</Platforms>`, `<AssemblyName>SeeDither</AssemblyName>`, `<RootNamespace>NINA.Plugin.SeeDither</RootNamespace>`.
2.4. Add `PackageReference` to `NINA.Plugin` version `3.2.0.9001` (mirror SeeDrift exactly). Set `<ExcludeAssets>runtime</ExcludeAssets>` on the reference.
2.5. Add an MSBuild `Target` named `CopyToNina` running `AfterTargets="Build"` that copies `$(TargetPath)` to `$(LOCALAPPDATA)\NINA\Plugins\3.0.0\SeeDither\`. Create the directory first if missing.
2.6. Mark `Resources.xaml` as `<Page>` with `Generator=MSBuild:Compile`.

### STEP 3 — File: `NINA.Plugin.SeeDither/Properties/AssemblyInfo.cs`
3.1. Add namespace `using System.Reflection;` and `using System.Runtime.InteropServices;`.
3.2. Add the following assembly attributes (values shown):
- `AssemblyTitle("SeeDither")`
- `AssemblyDescription("Absolute GoTo coordinate-offset dithering for Seestar S30/S50 mounts.")`
- `AssemblyCompany("")`, `AssemblyProduct("SeeDither")`, `AssemblyCopyright("Copyright © 2026")`
- `ComVisible(false)`
- `Guid("c4d2e5a1-7b3f-4e9d-9f2a-6f1c8b9a3d11")` (new unique GUID — generate fresh)
- `AssemblyVersion("1.0.0.0")`, `AssemblyFileVersion("1.0.0.0")`, `AssemblyInformationalVersion("1.0.0.0")`
3.3. Add MEF metadata required by NINA:
- `[assembly: AssemblyMetadata("Id", "c4d2e5a1-7b3f-4e9d-9f2a-6f1c8b9a3d11")]`
- `[assembly: AssemblyMetadata("Name", "SeeDither")]`
- `[assembly: AssemblyMetadata("Author", "Carl S.")]`
- `[assembly: AssemblyMetadata("Homepage", "")]`
- `[assembly: AssemblyMetadata("Repository", "")]`
- `[assembly: AssemblyMetadata("License", "MPL-2.0")]`
- `[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]`
- `[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.9001")]`
- `[assembly: AssemblyMetadata("ChangelogURL", "")]`
- `[assembly: AssemblyMetadata("FeaturedImageURL", "")]`
- `[assembly: AssemblyMetadata("ScreenshotURL", "")]`
- `[assembly: AssemblyMetadata("AltScreenshotURL", "")]`
- `[assembly: AssemblyMetadata("LongDescription", "Performs random absolute-coordinate dithering after exposures, designed for Seestar mounts whose guide pulses are unreliable.")]`
3.4. Cross-check against `C:\Users\carls\Documents\Dev\NINA_SeeDrift\NINA.Plugin.SeeDrift\Properties\AssemblyInfo.cs`; any metadata key present there but missing here must be added with sensible empty values.

### STEP 4 — File: `NINA.Plugin.SeeDither/Utility/SeeDitherLog.cs`
4.1. Namespace `NINA.Plugin.SeeDither.Utility`.
4.2. Create `internal static class SeeDitherLog`.
4.3. Add private const `string Tag = "[SeeDither] "`.
4.4. Add public static methods, each prefixing `Tag` and forwarding to `NINA.Core.Utility.Logger`:
- `void Info(string message)` → `Logger.Info(Tag + message)`
- `void Debug(string message)` → `Logger.Debug(Tag + message)`
- `void Warn(string message)` → `Logger.Warning(Tag + message)`
- `void Error(string message, Exception ex = null)` → if ex is null call `Logger.Error(Tag + message)`, otherwise `Logger.Error(Tag + message, ex)`.
4.5. All methods must be exception-safe: wrap body in `try { … } catch { /* swallow */ }` so logging never throws.

### STEP 5 — File: `NINA.Plugin.SeeDither/SeeDitherSettings.cs`
5.1. Namespace `NINA.Plugin.SeeDither`. Implement `INotifyPropertyChanged`.
5.2. Public class `SeeDitherSettings`. Mark with `[Serializable]`.
5.3. Backing fields and public properties (each property fires `PropertyChanged` in setter; setter calls private `Save()` after change):

| Property | Type | Default | Validation in setter |
|---|---|---|---|
| `Enabled` | `bool` | `true` | none |
| `ExposuresBetween` | `int` | `2` | clamp to `[1, 100]` |
| `MinOffsetArcsec` | `double` | `5.0` | clamp to `[0.1, 3600]`; if `>= MaxOffsetArcsec` set to `MaxOffsetArcsec - 0.1` |
| `MaxOffsetArcsec` | `double` | `60.0` | clamp to `[0.2, 3600]`; if `<= MinOffsetArcsec` set to `MinOffsetArcsec + 0.1` |
| `PlateScaleArcSecPerPx` | `double` | `2.39` | clamp to `[0.01, 100]` |
| `SlewSettleSeconds` | `double` | `2.0` | clamp to `[0, 30]` |

5.4. Add `protected void OnPropertyChanged([CallerMemberName] string name = null)`.
5.5. Add private static field `private static readonly string SettingsPath = Path.Combine(NINA.Core.Utility.CoreUtil.APPLICATIONTEMPPATH, "SeeDither", "settings.json");` (verify exact constant name from SeeDrift's settings file; mirror it).
5.6. Add `public static SeeDitherSettings Load()` returning a singleton-style loaded instance:
- If file missing or parse fails: return new defaults and call `Save()` on it.
- Use `System.Text.Json.JsonSerializer.Deserialize<SeeDitherSettings>(File.ReadAllText(SettingsPath))`.
- Wrap in try/catch → on exception log via `SeeDitherLog.Error` and return defaults.
5.7. Add `public void Save()`:
- Ensure directory exists (`Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath))`).
- Serialize `this` with `JsonSerializerOptions { WriteIndented = true }`.
- Write atomically: write to `SettingsPath + ".tmp"`, then `File.Move(tmp, SettingsPath, overwrite: true)`.
- Wrap in try/catch and log errors via `SeeDitherLog.Error`. Never throw out.
5.8. Constructor must set defaults via field initializers, **not** through setters (to avoid Save during deserialization). Add a private `bool _suspendSave` flag; `Save()` is a no-op when `_suspendSave == true`. Set the flag true during `Load()` deserialization, then false before returning.

### STEP 6 — File: `NINA.Plugin.SeeDither/SeeDitherPlugin.cs`
6.1. Namespace `NINA.Plugin.SeeDither`.
6.2. Class `SeeDitherPlugin` inherits `NINA.Plugin.PluginBase`.
6.3. Add `[Export(typeof(IPluginManifest))]` attribute on the class.
6.4. Constructor signature: `[ImportingConstructor] public SeeDitherPlugin(IProfileService profileService, ITelescopeMediator telescopeMediator)`.
6.5. In constructor:
- Call `base()` (PluginBase parameterless).
- Store `profileService` and `telescopeMediator` into `internal static` properties: `Mediators.ProfileService` and `Mediators.TelescopeMediator` (see Step 7).
- Call `Settings = SeeDitherSettings.Load()` and expose `public static SeeDitherSettings Settings { get; private set; }`.
- Wrap entire body in try/catch logging through `SeeDitherLog.Error`.
6.6. Override no additional methods unless required by `PluginBase`. Add `ToString()` returning `"SeeDither"`.

### STEP 7 — File: `NINA.Plugin.SeeDither/Mediators.cs`
7.1. Namespace `NINA.Plugin.SeeDither`.
7.2. `internal static class Mediators` exposing:
- `public static IProfileService ProfileService { get; set; }`
- `public static ITelescopeMediator TelescopeMediator { get; set; }`
7.3. No logic. Acts as a static handoff so the trigger (instantiated by NINA, not MEF-injected) can reach the mediators.

### STEP 8 — File: `NINA.Plugin.SeeDither/Utility/AstrometryOffset.cs`
8.1. Namespace `NINA.Plugin.SeeDither.Utility`.
8.2. `internal static class AstrometryOffset`.
8.3. Add `public static Coordinates ApplyOffset(Coordinates baseCoords, double deltaRaArcsec, double deltaDecArcsec)`:
- Inputs: `baseCoords` (NINA `NINA.Astrometry.Coordinates`), RA offset in arcseconds, Dec offset in arcseconds.
- Step a: convert Dec to radians: `decRad = baseCoords.Dec * Math.PI / 180.0`.
- Step b: compute `cosDec = Math.Cos(decRad)`; if `Math.Abs(cosDec) < 1e-6` set `cosDec = 1e-6` (pole guard).
- Step c: `deltaRaHours = (deltaRaArcsec / 3600.0 / 15.0) / cosDec`.
- Step d: `deltaDecDeg = deltaDecArcsec / 3600.0`.
- Step e: `newRaHours = baseCoords.RA + deltaRaHours; if (newRaHours < 0) newRaHours += 24; if (newRaHours >= 24) newRaHours -= 24;`.
- Step f: `newDecDeg = Math.Clamp(baseCoords.Dec + deltaDecDeg, -89.999, 89.999);`.
- Step g: return `new Coordinates(Angle.ByHours(newRaHours), Angle.ByDegree(newDecDeg), baseCoords.Epoch)`.
- Wrap entire method body in try/catch — on exception log and rethrow.
8.4. Add `public static (double raArcsec, double decArcsec) GenerateRandomOffset(Random rng, double minArcsec, double maxArcsec)`:
- Validate `maxArcsec > minArcsec`; if not, swap.
- Generate magnitude RA in `[minArcsec, maxArcsec]`, multiply by random sign (`rng.Next(2) == 0 ? -1 : 1`).
- Same for Dec, independently.
- Return tuple.

### STEP 9 — File: `NINA.Plugin.SeeDither/Sequencer/Triggers/SeeDitherAfterExposuresTrigger.cs`
9.1. Namespace `NINA.Plugin.SeeDither.Sequencer.Triggers`.
9.2. Required usings: `NINA.Sequencer.SequenceItem`, `NINA.Sequencer.Trigger`, `NINA.Sequencer.SequenceItem.Imaging`, `NINA.Core.Model`, `NINA.Astrometry`, `NINA.Equipment.Interfaces.Mediator`, `System.Threading`, `System.Threading.Tasks`, `System.ComponentModel.Composition`, `Newtonsoft.Json`.
9.3. Class declaration:
- `[ExportMetadata("Name", "SeeDither After Exposures")]`
- `[ExportMetadata("Description", "Dithers via absolute GoTo offsets; designed for Seestar mounts.")]`
- `[ExportMetadata("Icon", "DitherSVG")]` (matches key defined in Resources.xaml — Step 10)
- `[ExportMetadata("Category", "Telescope")]`
- `[Export(typeof(ISequenceTrigger))]`
- `[JsonObject(MemberSerialization.OptIn)]`
- `public class SeeDitherAfterExposuresTrigger : SequenceTrigger`
9.4. Private fields:
- `private readonly Random _rng = new Random();`
- `private int _exposureCounter = 0;`
- `private Coordinates _baseCoords = null;` (captured target coords)
- `private readonly object _stateLock = new object();`
9.5. Public JSON-serialized properties (each `[JsonProperty]`), bound to `SeeDitherPlugin.Settings` via getters/setters that read/write the static settings instance (so UI binding remains a single source of truth):
- `bool Enabled`
- `int ExposuresBetween`
- `double MinOffsetArcsec`
- `double MaxOffsetArcsec`
- `double PlateScaleArcSecPerPx`
- `double SlewSettleSeconds`

Each setter writes through to `SeeDitherPlugin.Settings.<X> = value;` and raises `RaisePropertyChanged()`. Each getter returns `SeeDitherPlugin.Settings.<X>`.

9.6. Constructors:
- `[ImportingConstructor] public SeeDitherAfterExposuresTrigger()` — calls `: base()`. No further work.
- `private SeeDitherAfterExposuresTrigger(SeeDitherAfterExposuresTrigger copyMe) : this()` — used by `Clone()`.

9.7. Override `public override object Clone()`:
- Return `new SeeDitherAfterExposuresTrigger(this) { Icon = Icon, Name = Name, Category = Category, Description = Description }`.

9.8. Override `public override void Initialize()`:
- Lock `_stateLock`. Reset `_exposureCounter = 0` and `_baseCoords = null`.
- Log via `SeeDitherLog.Info("Trigger initialized.")`.

9.9. Override `public override void Teardown()`:
- Lock `_stateLock`. Set `_baseCoords = null`, `_exposureCounter = 0`.
- Log `"Trigger torn down."`.

9.10. Override `public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem)`:
- Return `false` if `!Enabled`.
- Return `false` if `previousItem == null`.
- Return `false` unless `previousItem` is an exposure item — detect by checking type name contains `"TakeExposure"` OR `previousItem is IExposureItem` (use whichever NINA exposes; fallback to name check).
- Lock `_stateLock`, increment `_exposureCounter`.
- If `_exposureCounter % Math.Max(1, ExposuresBetween) != 0` return `false`.
- Else return `true`.
- Wrap in try/catch → log error, return `false`.

9.11. Override `public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token)`:
- Step a: `token.ThrowIfCancellationRequested();`
- Step b: Validate telescope mediator: `var telescope = Mediators.TelescopeMediator;` → if null, log error and `return;`.
- Step c: Query telescope info: `var info = telescope.GetInfo();` → if `info == null || !info.Connected`, log warning `"Telescope not connected; skipping dither."` and `return;`.
- Step d: Resolve base coordinates (locked under `_stateLock`):
  - If `_baseCoords == null`, walk up `context` parents to find a DSO container exposing `Target` / `Coordinates`. Use reflection-safe access: try `context` and `context.Parent` chain, looking for a property named `Target` whose value has a `InputCoordinates` or `Coordinates` property.
  - If found: `_baseCoords = <found Coordinates>.Clone();` (or `new Coordinates(angle, angle, epoch)` copy).
  - If not found: fall back to current telescope coordinates: `_baseCoords = new Coordinates(Angle.ByHours(info.RightAscension), Angle.ByDegree(info.Declination), Epoch.JNOW);` and log a warning that base coordinates were captured from telescope, not from target.
- Step e: Validate `MinOffsetArcsec < MaxOffsetArcsec`; if invalid, log error and `return;`.
- Step f: Generate offsets: `var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(_rng, MinOffsetArcsec, MaxOffsetArcsec);`.
- Step g: Compute target coords: `var target = AstrometryOffset.ApplyOffset(_baseCoords, raArc, decArc);`.
- Step h: Log: `"Dithering by RA={raArc:F1}\" Dec={decArc:F1}\" → RA={target.RAString} Dec={target.DecString}"`.
- Step i: Report progress: `progress?.Report(new ApplicationStatus { Status = "SeeDither: slewing offset" });`.
- Step j: Await `bool ok = await telescope.SlewToCoordinatesAsync(target, token);`. Wrap in try/catch capturing `OperationCanceledException` (rethrow) and generic Exception (log, set `ok = false`).
- Step k: If `!ok`, log warning `"SlewToCoordinatesAsync returned false."`.
- Step l: If `SlewSettleSeconds > 0`, `await Task.Delay(TimeSpan.FromSeconds(SlewSettleSeconds), token);`.
- Step m: `progress?.Report(new ApplicationStatus { Status = string.Empty });`.
- Step n: Final outer try/catch around steps b–m: on `OperationCanceledException` rethrow; on `Exception ex` call `SeeDitherLog.Error("Execute failed", ex)` and swallow (do not crash sequence).

9.12. Override `public override string ToString()` → returns `$"Category: {Category}, Item: {nameof(SeeDitherAfterExposuresTrigger)}, Enabled: {Enabled}, Every: {ExposuresBetween}, Range: [{MinOffsetArcsec},{MaxOffsetArcsec}] arcsec"`.

9.13. Override `public override bool Validate()`:
- Return `true` only if telescope info reports `Connected`. Otherwise add an issue string `"Telescope not connected."` to the `Issues` collection (use `Issues` list pattern from `SequenceTrigger`).
- Also validate `MinOffsetArcsec < MaxOffsetArcsec`, `ExposuresBetween >= 1`; emit issues if not.

### STEP 10 — File: `NINA.Plugin.SeeDither/Resources.xaml`
10.1. Define a `ResourceDictionary` with `xmlns` standard WPF + `xmlns:sys="clr-namespace:System;assembly=mscorlib"`.
10.2. Add a `<GeometryDrawing>`-based DrawingImage resource keyed `DitherSVG` representing a simple crosshair with four arrowheads pointing outward (N/E/S/W). Geometry data as a single `Path` `M`/`L` command string; do not embed external assets.
10.3. Add a `<DataTemplate DataType="{x:Type triggers:SeeDitherAfterExposuresTrigger}">` declaring the options UI:
- `xmlns:triggers="clr-namespace:NINA.Plugin.SeeDither.Sequencer.Triggers"`
- StackPanel containing labeled controls bound (TwoWay, UpdateSourceTrigger=PropertyChanged) to:
  - `Enabled` → CheckBox
  - `ExposuresBetween` → integer numeric up-down
  - `MinOffsetArcsec`, `MaxOffsetArcsec`, `PlateScaleArcSecPerPx`, `SlewSettleSeconds` → numeric up-down with two decimals
- Use NINA's standard control style keys if SeeDrift uses any (e.g., `LabeledControl`). Mirror naming exactly.

### STEP 11 — File: `NINA.Plugin.SeeDither/Resources.xaml.cs`
11.1. Partial class `Resources : ResourceDictionary` with parameterless constructor calling `InitializeComponent()`. No further logic.

### STEP 12 — Cross-file wiring verification
12.1. Confirm `SeeDitherPlugin.Settings` is initialized before the trigger's property getters run. If NINA constructs triggers before plugin instantiation, in each settings-backed property getter add a null guard: `if (SeeDitherPlugin.Settings == null) return <default>;`.
12.2. Confirm `[Export(typeof(ISequenceTrigger))]` is the correct export contract used by NINA (cross-reference SeeDrift's exported sequence items). If SeeDrift exports `ISequenceItem` instead, the trigger must use `ISequenceTrigger` — keep the trigger export distinct.
12.3. Confirm `SequenceTrigger` base class namespace is `NINA.Sequencer.Trigger` — adjust using if different.

### STEP 13 — Build and deploy
13.1. From repo root run `dotnet restore NINA.Plugin.SeeDither.sln`.
13.2. Run `dotnet build NINA.Plugin.SeeDither.sln -c Release`.
13.3. Verify the `CopyToNina` target copied `SeeDither.dll` to `%LOCALAPPDATA%\NINA\Plugins\3.0.0\SeeDither\`.
13.4. Start NINA; verify in Plugin Manager that "SeeDither" appears with version `1.0.0.0`.
13.5. In an Advanced Sequencer DSO container, under the trigger picker → Telescope category, confirm "SeeDither After Exposures" is selectable.

### STEP 14 — Manual validation checklist (no code; verify behavior)
14.1. Run sequence with telescope disconnected → trigger logs warning, sequence continues, no crash.
14.2. Run sequence with `ExposuresBetween = 2` → confirm slew occurs after exposures #2, #4, #6.
14.3. Confirm RA offset magnitude scales with `1/cos(Dec)` by inspecting log lines at Dec=0° vs Dec=60°.
14.4. Cancel sequence mid-slew → confirm `OperationCanceledException` propagates and `Teardown()` runs.

---

End of blueprint. The executing model must implement files in the exact order STEP 1 → STEP 14, completing every numbered substep before advancing.

---

