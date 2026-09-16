using NINA.Plugin.SeeDither;
using System.IO;
using Newtonsoft.Json;

namespace NINA.Plugin.SeeDither.Tests;

public class SeeDitherSettingsLoadSaveTests : IDisposable {
    private readonly string _tempDir;
    private readonly string _settingsPath;
    private readonly string _previousOverride;

    public SeeDitherSettingsLoadSaveTests() {
        _tempDir = Path.Combine(Path.GetTempPath(), "SeeDitherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
        _previousOverride = SeeDitherSettings.SettingsPathOverride;
        SeeDitherSettings.SettingsPathOverride = _settingsPath;
    }

    public void Dispose() {
        SeeDitherSettings.SettingsPathOverride = _previousOverride;
        if (Directory.Exists(_tempDir)) {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsSelectedScopeName() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 100;
        settings.MaxOffsetArcsec = 300;
        settings.ExposuresBetween = 5;
        settings.SelectedScopeName = "Seestar S50 Pro";

        // Act
        settings.Save();
        var loaded = SeeDitherSettings.Load();

        // Assert: Selector and everything else survives the round trip
        Assert.Equal("Seestar S50 Pro", loaded.SelectedScopeName);
        Assert.Equal(2.30, loaded.PlateScaleArcSecPerPx, 2);
        Assert.Equal(100, loaded.MinOffsetArcsec);
        Assert.Equal(300, loaded.MaxOffsetArcsec);
        Assert.Equal(5, loaded.ExposuresBetween);
    }

    [Fact]
    public void Save_CreatesJsonFileWithSelectorAndDerivedScale() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.SelectedScopeName = "Seestar S50";

        // Act
        settings.Save();

        // Assert: File exists and contains the persisted selector plus the
        // plate scale derived from it
        Assert.True(File.Exists(_settingsPath));
        var loadedJson = File.ReadAllText(_settingsPath);
        dynamic loaded = JsonConvert.DeserializeObject(loadedJson)!;

        Assert.NotNull(loaded);
        Assert.Equal("Seestar S50", (string)loaded.SelectedScopeName);
        Assert.Equal(2.39, (double)loaded.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithValidJson_RestoresSettings() {
        // Arrange
        var json = @"{
            ""Enabled"": false,
            ""ExposuresBetween"": 10,
            ""MinOffsetArcsec"": 50,
            ""MaxOffsetArcsec"": 250,
            ""SelectedScopeName"": ""Seestar S50 Pro""
        }";
        File.WriteAllText(_settingsPath, json);

        // Act
        var loaded = SeeDitherSettings.Load();

        // Assert
        Assert.False(loaded.Enabled);
        Assert.Equal(10, loaded.ExposuresBetween);
        Assert.Equal(50, loaded.MinOffsetArcsec);
        Assert.Equal(250, loaded.MaxOffsetArcsec);
        Assert.Equal("Seestar S50 Pro", loaded.SelectedScopeName);
        Assert.Equal(2.30, loaded.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithOlderJsonMissingSelector_DefaultsToS30() {
        // Arrange: JSON as an older version wrote it — camera-detected plate
        // scale, no selector present
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 2,
            ""MinOffsetArcsec"": 20,
            ""MaxOffsetArcsec"": 150,
            ""PlateScaleArcSecPerPx"": 2.39
        }";
        File.WriteAllText(_settingsPath, json);

        // Act
        var loaded = SeeDitherSettings.Load();

        // Assert: Gracefully falls back to the default scope (S30 / current default)
        Assert.Equal("Seestar S30", loaded.SelectedScopeName);
        Assert.Equal(3.99, loaded.PlateScaleArcSecPerPx, 2);
        Assert.Equal(20, loaded.MinOffsetArcsec);
        Assert.Equal(150, loaded.MaxOffsetArcsec);
    }

    [Fact]
    public void Load_WithUnknownSelectorName_DefaultsToS30() {
        // Arrange: Hand-edited or corrupt selector name
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 2,
            ""MinOffsetArcsec"": 20,
            ""MaxOffsetArcsec"": 150,
            ""SelectedScopeName"": ""Bogus telescope""
        }";
        File.WriteAllText(_settingsPath, json);

        // Act
        var loaded = SeeDitherSettings.Load();

        // Assert: Unknown names normalize to the default scope
        Assert.Equal("Seestar S30", loaded.SelectedScopeName);
        Assert.Equal(3.99, loaded.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithOutOfRangeValues_ClampsToValidRange() {
        // Arrange: JSON with out-of-range values
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 150,
            ""MinOffsetArcsec"": 600,
            ""MaxOffsetArcsec"": 700,
            ""SelectedScopeName"": ""Seestar S50""
        }";
        File.WriteAllText(_settingsPath, json);

        // Act
        var loaded = SeeDitherSettings.Load();

        // Assert: Values should be clamped
        Assert.Equal(100, loaded.ExposuresBetween); // Max is 100
        Assert.Equal(500, loaded.MinOffsetArcsec); // Max is 500
        Assert.Equal(500, loaded.MaxOffsetArcsec); // Max is 500
        Assert.Equal("Seestar S50", loaded.SelectedScopeName);
        Assert.Equal(2.39, loaded.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithMinGreaterThanMax_AdjustsMin() {
        // Arrange: JSON where Min > Max
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 2,
            ""MinOffsetArcsec"": 300,
            ""MaxOffsetArcsec"": 150,
            ""SelectedScopeName"": ""Seestar S30 Pro""
        }";
        File.WriteAllText(_settingsPath, json);

        // Act
        var loaded = SeeDitherSettings.Load();

        // Assert: Min should be adjusted to match Max
        Assert.Equal(150, loaded.MinOffsetArcsec);
        Assert.Equal(150, loaded.MaxOffsetArcsec);
        Assert.Equal("Seestar S30 Pro", loaded.SelectedScopeName);
    }

    [Fact]
    public void SerializationRoundTrip_PreservesSelectorAndDerivedScale() {
        // Arrange
        var original = new SeeDitherSettings();
        original.Enabled = false;
        original.ExposuresBetween = 7;
        original.SelectedScopeName = "Seestar S50";
        // Use Text properties to set Min/Max (since internal setters aren't accessible in serialization)
        original.MinOffsetArcsecText = "75";
        original.MaxOffsetArcsecText = "225";

        // Act: Serialize to JSON
        var json = JsonConvert.SerializeObject(original);
        var dto = JsonConvert.DeserializeObject<dynamic>(json)!;

        // Assert: Verify JSON contains expected values
        Assert.NotNull(dto);
        Assert.False((bool)dto.Enabled);
        Assert.Equal(7, (int)dto.ExposuresBetween);
        Assert.Equal(75, (int)dto.MinOffsetArcsec);
        Assert.Equal(225, (int)dto.MaxOffsetArcsec);
        Assert.Equal("Seestar S50", (string)dto.SelectedScopeName);
        Assert.Equal(2.39, (double)dto.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsDefaults() {
        // Act: No settings file exists
        var loaded = SeeDitherSettings.Load();

        // Assert: Verify default values
        Assert.True(loaded.Enabled);
        Assert.Equal(2, loaded.ExposuresBetween);
        Assert.Equal(20, loaded.MinOffsetArcsec);
        Assert.Equal(150, loaded.MaxOffsetArcsec);
        Assert.Equal("Seestar S30", loaded.SelectedScopeName);
        Assert.Equal(3.99, loaded.PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void Load_WithInvalidJson_ReturnsDefaults() {
        // Arrange: Invalid JSON
        File.WriteAllText(_settingsPath, "{ this is not valid json }");

        // Act & Assert: Should not throw, should return defaults
        var loaded = SeeDitherSettings.Load();
        Assert.True(loaded.Enabled);
        Assert.Equal("Seestar S30", loaded.SelectedScopeName);
        Assert.Equal(3.99, loaded.PlateScaleArcSecPerPx, 2);
    }
}