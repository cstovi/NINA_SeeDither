using NINA.Astrometry;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither.Tests;

public class AstrometryOffsetTests {
    [Fact]
    public void ApplyOffset_WithKnownValues_ProducesCorrectCoordinates() {
        // Arrange: Base position at RA=00:09:30, Dec=29° 51' 11"
        var baseCoords = new Coordinates(
            Angle.ByHours(0.158333), // 0h 9m 30s
            Angle.ByDegree(29.853056), // 29° 51' 11"
            Epoch.JNOW
        );
        double deltaRaArcsec = -482.8;
        double deltaDecArcsec = 397.9;

        // Act
        var result = AstrometryOffset.ApplyOffset(baseCoords, deltaRaArcsec, deltaDecArcsec);

        // Assert: Expected RA=00:08:53, Dec=29° 57' 49"
        Assert.Equal(0.148, result.RA, 3); // 0h 8m 52.9s ≈ 0.148 hours
        Assert.Equal(29.964, result.Dec, 3); // 29° 57' 49" ≈ 29.964°
    }

    [Fact]
    public void ApplyOffset_AtEquator_ScalesRACorrectly() {
        // Arrange: At equator, cos(Dec) = 1, so RA scaling is minimal
        var baseCoords = new Coordinates(
            Angle.ByHours(12.0), // 12h
            Angle.ByDegree(0.0), // 0° (equator)
            Epoch.JNOW
        );
        double deltaRaArcsec = 3600.0; // 1 hour of RA at equator = 3600" * 15 = 54000"
        double deltaDecArcsec = 0.0;

        // Act
        var result = AstrometryOffset.ApplyOffset(baseCoords, deltaRaArcsec, deltaDecArcsec);

        // Assert: RA should increase by 3600/54000 = 0.0667 hours
        Assert.Equal(12.0667, result.RA, 4);
        Assert.Equal(0.0, result.Dec, 4);
    }

    [Fact]
    public void ApplyOffset_NearPole_ScalesRACorrectly() {
        // Arrange: Near pole, cos(Dec) is small, so RA changes are amplified
        var baseCoords = new Coordinates(
            Angle.ByHours(12.0), // 12h
            Angle.ByDegree(80.0), // 80° (near pole)
            Epoch.JNOW
        );
        double deltaRaArcsec = 500.0;
        double deltaDecArcsec = 0.0;

        // Act
        var result = AstrometryOffset.ApplyOffset(baseCoords, deltaRaArcsec, deltaDecArcsec);

        // Assert: cos(80°) ≈ 0.1736, so deltaRaHours = (500/54000)/0.1736 ≈ 0.0533 hours
        Assert.Equal(12.0533, result.RA, 3);
        Assert.Equal(80.0, result.Dec, 4);
    }

    [Fact]
    public void ApplyOffset_RAWrapsAround24Hours() {
        // Arrange: RA near 0h, negative offset should wrap to 23h+
        var baseCoords = new Coordinates(
            Angle.ByHours(0.5), // 0h 30m
            Angle.ByDegree(30.0),
            Epoch.JNOW
        );
        double deltaRaArcsec = -5400.0; // Large negative offset
        double deltaDecArcsec = 0.0;

        // Act
        var result = AstrometryOffset.ApplyOffset(baseCoords, deltaRaArcsec, deltaDecArcsec);

        // Assert: RA should wrap around (stay in 0-24 range)
        Assert.InRange(result.RA, 0.0, 24.0);
    }

    [Fact]
    public void ApplyOffset_DecClampedAtPoles() {
        // Arrange: Dec near pole, large positive offset should clamp
        var baseCoords = new Coordinates(
            Angle.ByHours(12.0),
            Angle.ByDegree(89.0), // Near north pole
            Epoch.JNOW
        );
        double deltaRaArcsec = 0.0;
        double deltaDecArcsec = 7200.0; // 2 degrees, would exceed 90°

        // Act
        var result = AstrometryOffset.ApplyOffset(baseCoords, deltaRaArcsec, deltaDecArcsec);

        // Assert: Dec should be clamped to 89.999
        Assert.Equal(89.999, result.Dec, 3);
    }

    [Fact]
    public void GenerateRandomOffset_WithMinEqualsMax_ProducesExactMagnitude() {
        // Arrange
        var rng = new Random(42); // Seeded for deterministic results
        double minArcsec = 500.0;
        double maxArcsec = 500.0;

        // Act
        var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(rng, minArcsec, maxArcsec);

        // Assert: Magnitudes should be exactly 500 (signs can be ± but magnitude is fixed)
        Assert.Equal(500.0, Math.Abs(raArc), 6);
        Assert.Equal(500.0, Math.Abs(decArc), 6);
    }

    [Fact]
    public void GenerateRandomOffset_WithRange_ProducesMagnitudeInRange() {
        // Arrange
        var rng = new Random(42);
        double minArcsec = 100.0;
        double maxArcsec = 200.0;

        // Act: Generate multiple offsets to test range
        for (int i = 0; i < 10; i++) {
            var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(rng, minArcsec, maxArcsec);

            // Assert: Magnitudes should be in [100, 200]
            Assert.InRange(Math.Abs(raArc), minArcsec, maxArcsec);
            Assert.InRange(Math.Abs(decArc), minArcsec, maxArcsec);
        }
    }

    [Fact]
    public void GenerateRandomOffset_ProducesRandomSigns() {
        // Arrange
        var rng = new Random(42);
        double minArcsec = 100.0;
        double maxArcsec = 200.0;

        // Act: Generate multiple offsets
        var offsets = new List<(double ra, double dec)>();
        for (int i = 0; i < 20; i++) {
            offsets.Add(AstrometryOffset.GenerateRandomOffset(rng, minArcsec, maxArcsec));
        }

        // Assert: Should have both positive and negative values (not all same sign)
        var hasPositiveRA = offsets.Any(o => o.ra > 0);
        var hasNegativeRA = offsets.Any(o => o.ra < 0);
        var hasPositiveDec = offsets.Any(o => o.dec > 0);
        var hasNegativeDec = offsets.Any(o => o.dec < 0);

        Assert.True(hasPositiveRA, "Should have at least one positive RA offset");
        Assert.True(hasNegativeRA, "Should have at least one negative RA offset");
        Assert.True(hasPositiveDec, "Should have at least one positive Dec offset");
        Assert.True(hasNegativeDec, "Should have at least one negative Dec offset");
    }

    [Fact]
    public void GenerateRandomOffset_WithMinGreaterThanMax_SwapsThem() {
        // Arrange
        var rng = new Random(42);
        double minArcsec = 200.0;
        double maxArcsec = 100.0; // Swapped

        // Act
        var (raArc, decArc) = AstrometryOffset.GenerateRandomOffset(rng, minArcsec, maxArcsec);

        // Assert: Should still produce valid offsets in [100, 200]
        Assert.InRange(Math.Abs(raArc), 100.0, 200.0);
        Assert.InRange(Math.Abs(decArc), 100.0, 200.0);
    }
}
