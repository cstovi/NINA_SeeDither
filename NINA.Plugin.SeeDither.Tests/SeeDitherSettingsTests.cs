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
    public void PlateScaleArcSecPerPx_ClampsToValidRange() {
        // Arrange
        var settings = new SeeDitherSettings();

        // Act & Assert: Below minimum
        settings.PlateScaleArcSecPerPx = 0.001;
        Assert.Equal(0.01, settings.PlateScaleArcSecPerPx);

        // Act & Assert: Above maximum
        settings.PlateScaleArcSecPerPx = 150.0;
        Assert.Equal(100.0, settings.PlateScaleArcSecPerPx);

        // Act & Assert: Valid value
        settings.PlateScaleArcSecPerPx = 3.74;
        Assert.Equal(3.74, settings.PlateScaleArcSecPerPx);
    }

    [Fact]
    public void MinOffsetPixels_CalculatesCorrectly() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MinOffsetArcsec = 100;
        settings.PlateScaleArcSecPerPx = 2.0;

        // Act
        var pixels = settings.MinOffsetPixels;

        // Assert: 100 arcsec / 2.0 arcsec/px = 50 px
        Assert.Equal(50.0, pixels, 6);
    }

    [Fact]
    public void MaxOffsetPixels_CalculatesCorrectly() {
        // Arrange
        var settings = new SeeDitherSettings();
        settings.MaxOffsetArcsec = 200;
        settings.PlateScaleArcSecPerPx = 4.0;

        // Act
        var pixels = settings.MaxOffsetPixels;

        // Assert: 200 arcsec / 4.0 arcsec/px = 50 px
        Assert.Equal(50.0, pixels, 6);
    }
}
