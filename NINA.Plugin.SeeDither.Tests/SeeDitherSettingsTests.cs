using NINA.Plugin.SeeDither;

namespace NINA.Plugin.SeeDither.Tests;

public class SeeDitherSettingsTests {
    [Fact]
    public void MinOffsetArcsec_WhenSetAboveMax_AutoRaisesMax() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MaxOffsetArcsec = 150;

        // Act
        settings.MinOffsetArcsec = 500;

        // Assert
        Assert.Equal(500, settings.MinOffsetArcsec);
        Assert.Equal(500, settings.MaxOffsetArcsec); // Max should auto-raise
    }

    [Fact]
    public void MaxOffsetArcsec_WhenSetBelowMin_AutoDropsMin() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 200;

        // Act
        settings.MaxOffsetArcsec = 100;

        // Assert
        Assert.Equal(100, settings.MinOffsetArcsec); // Min should auto-drop
        Assert.Equal(100, settings.MaxOffsetArcsec);
    }

    [Fact]
    public void MinOffsetArcsec_EqualsMax_IsAllowed() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act
        settings.MinOffsetArcsec = 500;
        settings.MaxOffsetArcsec = 500;

        // Assert
        Assert.Equal(500, settings.MinOffsetArcsec);
        Assert.Equal(500, settings.MaxOffsetArcsec);
    }

    [Fact]
    public void MinOffsetArcsec_ClampsToValidRange() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        settings.MinOffsetArcsec = 0;
        Assert.Equal(1, settings.MinOffsetArcsec);

        // Act & Assert: Above maximum
        settings.MinOffsetArcsec = 600;
        Assert.Equal(500, settings.MinOffsetArcsec);
    }

    [Fact]
    public void MaxOffsetArcsec_ClampsToValidRange() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        settings.MaxOffsetArcsec = 0;
        Assert.Equal(1, settings.MaxOffsetArcsec);

        // Act & Assert: Above maximum
        settings.MaxOffsetArcsec = 600;
        Assert.Equal(500, settings.MaxOffsetArcsec);
    }

    [Fact]
    public void MinOffsetArcsecText_WithValidInput_UpdatesMinOffsetArcsec() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act
        settings.MinOffsetArcsecText = "250";

        // Assert
        Assert.Equal(250, settings.MinOffsetArcsec);
        Assert.Equal("250", settings.MinOffsetArcsecText);
    }

    [Fact]
    public void MinOffsetArcsecText_WithInvalidNumber_ThrowsFormatException() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert
        Assert.Throws<FormatException>(() => settings.MinOffsetArcsecText = "abc");
    }

    [Fact]
    public void MinOffsetArcsecText_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MinOffsetArcsecText = "0");

        // Act & Assert: Above maximum
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MinOffsetArcsecText = "600");
    }

    [Fact]
    public void MaxOffsetArcsecText_WithValidInput_UpdatesMaxOffsetArcsec() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act
        settings.MaxOffsetArcsecText = "300";

        // Assert
        Assert.Equal(300, settings.MaxOffsetArcsec);
        Assert.Equal("300", settings.MaxOffsetArcsecText);
    }

    [Fact]
    public void MaxOffsetArcsecText_WithInvalidNumber_ThrowsFormatException() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert
        Assert.Throws<FormatException>(() => settings.MaxOffsetArcsecText = "xyz");
    }

    [Fact]
    public void MaxOffsetArcsecText_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxOffsetArcsecText = "0");

        // Act & Assert: Above maximum
        Assert.Throws<ArgumentOutOfRangeException>(() => settings.MaxOffsetArcsecText = "600");
    }

    [Fact]
    public void MinOffsetArcsecText_WithEmptyString_DoesNotThrow() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 100;

        // Act
        settings.MinOffsetArcsecText = "";

        // Assert: Should not throw, value should remain unchanged
        Assert.Equal(100, settings.MinOffsetArcsec);
    }

    [Fact]
    public void ExposuresBetween_ClampsToValidRange() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        settings.ExposuresBetween = 0;
        Assert.Equal(1, settings.ExposuresBetween);

        // Act & Assert: Above maximum
        settings.ExposuresBetween = 150;
        Assert.Equal(100, settings.ExposuresBetween);

        // Act & Assert: Valid value
        settings.ExposuresBetween = 5;
        Assert.Equal(5, settings.ExposuresBetween);
    }

    [Fact]
    public void PlateScaleArcSecPerPx_IsDerivedFromSelectedScope() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Assert: Default scope is Seestar S30
        Assert.Equal("Seestar S30", settings.SelectedScopeName);
        Assert.Equal(3.99, settings.PlateScaleArcSecPerPx);

        // Act & Assert: Each scope maps to its documented plate scale
        settings.SelectedScopeName = "Seestar S30 Pro";
        Assert.Equal(3.74, settings.PlateScaleArcSecPerPx);

        settings.SelectedScopeName = "Seestar S50";
        Assert.Equal(2.39, settings.PlateScaleArcSecPerPx);

        settings.SelectedScopeName = "Seestar S50 Pro";
        Assert.Equal(2.30, settings.PlateScaleArcSecPerPx);
    }

    [Fact]
    public void SelectedScopeName_WithUnknownName_NormalizesToDefault() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act
        settings.SelectedScopeName = "Some Other Camera";

        // Assert: Falls back to the default scope (Seestar S30)
        Assert.Equal("Seestar S30", settings.SelectedScopeName);
        Assert.Equal(3.99, settings.PlateScaleArcSecPerPx);
    }

    [Fact]
    public void SelectedScopeName_WhenChanged_RaisesPropertyChangedForPixelEstimates() {
        // Arrange
        var settings = new SeeDitherSettings();
        var changed = new List<string>();
        settings.PropertyChanged += (s, e) => changed.Add(e.PropertyName ?? string.Empty);

        // Act
        settings.SelectedScopeName = "Seestar S50";

        // Assert: The selector, its derived scale, and both estimates notify
        Assert.Contains(nameof(SeeDitherSettings.SelectedScopeName), changed);
        Assert.Contains(nameof(SeeDitherSettings.PlateScaleArcSecPerPx), changed);
        Assert.Contains(nameof(SeeDitherSettings.MinOffsetPixels), changed);
        Assert.Contains(nameof(SeeDitherSettings.MaxOffsetPixels), changed);
    }

    [Fact]
    public void SelectedScopeName_WhenChanged_UpdatesPixelEstimates() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 100;
        settings.MaxOffsetArcsec = 200;
        var minBefore = settings.MinOffsetPixels;

        // Act
        settings.SelectedScopeName = "Seestar S50"; // 2.39 arcsec/px

        // Assert: Estimates recompute against the new plate scale
        Assert.NotEqual(minBefore, settings.MinOffsetPixels);
        Assert.Equal(100.0 / 2.39, settings.MinOffsetPixels, 6);
        Assert.Equal(200.0 / 2.39, settings.MaxOffsetPixels, 6);
    }

    [Fact]
    public void MinOffsetPixels_CalculatesCorrectly() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 100;
        settings.SelectedScopeName = "Seestar S50"; // 2.39 arcsec/px

        // Act
        var pixels = settings.MinOffsetPixels;

        // Assert: 100 arcsec / 2.39 arcsec/px ≈ 41.84 px
        Assert.Equal(100.0 / 2.39, pixels, 6);
    }

    [Fact]
    public void MaxOffsetPixels_CalculatesCorrectly() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MaxOffsetArcsec = 200;
        settings.SelectedScopeName = "Seestar S30 Pro"; // 3.74 arcsec/px

        // Act
        var pixels = settings.MaxOffsetPixels;

        // Assert: 200 arcsec / 3.74 arcsec/px ≈ 53.48 px
        Assert.Equal(200.0 / 3.74, pixels, 6);
    }

    [Theory]
    [InlineData("Seestar S30", 3.99)]
    [InlineData("Seestar S30 Pro", 3.74)]
    [InlineData("Seestar S50", 2.39)]
    [InlineData("Seestar S50 Pro", 2.30)]
    public void SeestarScope_FromName_MapsToPlateScale(string scopeName, double expectedPlateScale) {
        Assert.Equal(expectedPlateScale, SeestarScope.FromName(scopeName).PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void SeestarScope_FromName_UnknownName_ReturnsDefault() {
        Assert.Equal(SeestarScope.Default.DisplayName, SeestarScope.FromName("Not a Seestar").DisplayName);
        Assert.Equal(3.99, SeestarScope.FromName("Not a Seestar").PlateScaleArcSecPerPx, 2);
    }

    [Fact]
    public void SeestarScope_FromName_NullName_ReturnsDefault() {
        Assert.Equal(SeestarScope.Default.DisplayName, SeestarScope.FromName(null).DisplayName);
    }
}
