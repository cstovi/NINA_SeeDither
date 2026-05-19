using System;
using NINA.Astrometry;
using NINA.Plugin.SeeDither.Utility;

namespace NINA.Plugin.SeeDither.Utility {
    internal static class AstrometryOffset {
        public static Coordinates ApplyOffset(Coordinates baseCoords, double deltaRaArcsec, double deltaDecArcsec) {
            try {
                double decRad = baseCoords.Dec * Math.PI / 180.0;
                double cosDec = Math.Cos(decRad);
                if (Math.Abs(cosDec) < 1e-6) cosDec = 1e-6;

                double deltaRaHours = (deltaRaArcsec / 3600.0 / 15.0) / cosDec;
                double deltaDecDeg = deltaDecArcsec / 3600.0;

                double newRaHours = baseCoords.RA + deltaRaHours;
                while (newRaHours < 0) newRaHours += 24;
                while (newRaHours >= 24) newRaHours -= 24;

                double newDecDeg = Math.Clamp(baseCoords.Dec + deltaDecDeg, -89.999, 89.999);

                return new Coordinates(Angle.ByHours(newRaHours), Angle.ByDegree(newDecDeg), baseCoords.Epoch);
            } catch (Exception ex) {
                SeeDitherLog.Error("ApplyOffset failed", ex);
                throw;
            }
        }

        public static (double raArcsec, double decArcsec) GenerateRandomOffset(Random rng, double minArcsec, double maxArcsec) {
            if (maxArcsec < minArcsec) {
                double temp = minArcsec;
                minArcsec = maxArcsec;
                maxArcsec = temp;
            }

            double raMag = minArcsec + rng.NextDouble() * (maxArcsec - minArcsec);
            double decMag = minArcsec + rng.NextDouble() * (maxArcsec - minArcsec);

            if (rng.Next(2) == 0) raMag = -raMag;
            if (rng.Next(2) == 0) decMag = -decMag;

            return (raMag, decMag);
        }
    }
}
