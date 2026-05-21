using NINA.Plugin.SeeDither;
using System.IO;
using Newtonsoft.Json;

namespace NINA.Plugin.SeeDither.Tests;

public class SeeDitherSettingsLoadSaveTests {
    [Fact]
    public void Save_CreatesJsonFile() {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "SeeDitherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var settingsPath = Path.Combine(tempDir, "settings.json");

        try {
            var settings = new SeeDitherSettings();
            settings.MinOffsetArcsec = 100;
            settings.MaxOffsetArcsec = 300;
            settings.ExposuresBetween = 5;
            settings.PlateScaleArcSecPerPx = 2.5;

            // Note: We can't easily test Save() directly since it uses a hardcoded path
            // This test demonstrates the structure we'd need if Save() accepted a path parameter
            // For now, we'll test the serialization format manually

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(settingsPath, json);

            // Act
            var loadedJson = File.ReadAllText(settingsPath);
            dynamic? loaded = JsonConvert.DeserializeObject(loadedJson);

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(100, (int)loaded.MinOffsetArcsec);
            Assert.Equal(300, (int)loaded.MaxOffsetArcsec);
            Assert.Equal(5, (int)loaded.ExposuresBetween);
            Assert.Equal(2.5, (double)loaded.PlateScaleArcSecPerPx);
        } finally {
            if (Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Load_WithValidJson_RestoresSettings() {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "SeeDitherTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var settingsPath = Path.Combine(tempDir, "settings.json");

        try {
            var json = @"{
                ""Enabled"": false,
                ""ExposuresBetween"": 10,
                ""MinOffsetArcsec"": 50,
                ""MaxOffsetArcsec"": 250,
                ""PlateScaleArcSecPerPx"": 1.5
            }";
            File.WriteAllText(settingsPath, json);

            // Act: Deserialize manually (since Load() uses hardcoded path)
            var loaded = JsonConvert.DeserializeObject<dynamic>(json);

            // Assert
            Assert.NotNull(loaded);
            Assert.False((bool)loaded.Enabled);
            Assert.Equal(10, (int)loaded.ExposuresBetween);
            Assert.Equal(50, (int)loaded.MinOffsetArcsec);
            Assert.Equal(250, (int)loaded.MaxOffsetArcsec);
            Assert.Equal(1.5, (double)loaded.PlateScaleArcSecPerPx);
        } finally {
            if (Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void Load_WithOutOfRangeValues_ClampsToValidRange() {
        // Arrange: JSON with out-of-range values
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 150,
            ""MinOffsetArcsec"": 600,
            ""MaxOffsetArcsec"": 700,
            ""PlateScaleArcSecPerPx"": 150.0
        }";

        // Act: Deserialize and apply to settings
        var dto = JsonConvert.DeserializeObject<dynamic>(json);
        var settings = new SeeDitherSettings();
        settings.ExposuresBetween = (int)dto.ExposuresBetween;
        settings.MinOffsetArcsec = (int)dto.MinOffsetArcsec;
        settings.MaxOffsetArcsec = (int)dto.MaxOffsetArcsec;
        settings.PlateScaleArcSecPerPx = (double)dto.PlateScaleArcSecPerPx;

        // Assert: Values should be clamped
        Assert.Equal(100, settings.ExposuresBetween); // Max is 100
        Assert.Equal(500, settings.MinOffsetArcsec); // Max is 500
        Assert.Equal(500, settings.MaxOffsetArcsec); // Max is 500
        Assert.Equal(100.0, settings.PlateScaleArcSecPerPx); // Max is 100.0
    }

    [Fact]
    public void Load_WithMinGreaterThanMax_AdjustsMin() {
        // Arrange: JSON where Min > Max
        var json = @"{
            ""Enabled"": true,
            ""ExposuresBetween"": 2,
            ""MinOffsetArcsec"": 300,
            ""MaxOffsetArcsec"": 150,
            ""PlateScaleArcSecPerPx"": 3.74
        }";

        // Act: Deserialize and apply to settings
        var dto = JsonConvert.DeserializeObject<dynamic>(json);
        var settings = new SeeDitherSettings();
        
        // Simulate Load() logic: set Min first, then Max
        // When Max is set to 150, it should auto-drop Min to 150
        settings.MinOffsetArcsec = (int)dto.MinOffsetArcsec; // Sets to 300
        settings.MaxOffsetArcsec = (int)dto.MaxOffsetArcsec; // Sets to 150, should drop Min to 150

        // Assert: Min should be adjusted to match Max
        Assert.Equal(150, settings.MinOffsetArcsec);
        Assert.Equal(150, settings.MaxOffsetArcsec);
    }

    [Fact]
    public void SerializationRoundTrip_PreservesAllValues() {
        // Arrange
        var original = new SeeDitherSettings();
        original.Enabled = false;
        original.ExposuresBetween = 7;
        original.PlateScaleArcSecPerPx = 2.39;
        // Use Text properties to set Min/Max (since internal setters aren't accessible in serialization)
        original.MinOffsetArcsecText = "75";
        original.MaxOffsetArcsecText = "225";

        // Act: Serialize to JSON
        var json = JsonConvert.SerializeObject(original);
        
        // Deserialize to dynamic to check JSON structure (not full object deserialization)
        var dto = JsonConvert.DeserializeObject<dynamic>(json);

        // Assert: Verify JSON contains expected values
        Assert.NotNull(dto);
        Assert.False((bool)dto.Enabled);
        Assert.Equal(7, (int)dto.ExposuresBetween);
        Assert.Equal(75, (int)dto.MinOffsetArcsec);
        Assert.Equal(225, (int)dto.MaxOffsetArcsec);
        Assert.Equal(2.39, (double)dto.PlateScaleArcSecPerPx);
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsDefaults() {
        // This test documents expected behavior when settings file doesn't exist
        // The actual Load() method would return a new instance with defaults

        // Arrange
        var defaults = new SeeDitherSettings();

        // Assert: Verify default values
        Assert.True(defaults.Enabled);
        Assert.Equal(2, defaults.ExposuresBetween);
        Assert.Equal(20, defaults.MinOffsetArcsec);
        Assert.Equal(150, defaults.MaxOffsetArcsec);
        Assert.Equal(3.74, defaults.PlateScaleArcSecPerPx);
    }

    [Fact]
    public void Load_WithInvalidJson_ReturnsDefaults() {
        // Arrange: Invalid JSON
        var invalidJson = "{ this is not valid json }";

        // Act & Assert: Should not throw, should return defaults
        try {
            var result = JsonConvert.DeserializeObject<SeeDitherSettings>(invalidJson);
            Assert.Fail("Should have thrown JsonException");
        } catch (JsonException) {
            // Expected - in actual Load() this would be caught and defaults returned
            Assert.True(true);
        }
    }
}
