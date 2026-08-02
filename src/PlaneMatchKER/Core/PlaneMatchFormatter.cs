// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

using System;

namespace PlaneMatchKER.Core
{
    internal static class PlaneMatchFormatter
    {
        internal static string SignedDistance(double metres)
        {
            if (double.IsNaN(metres))
            {
                return "—";
            }

            string sign =
                metres > 0.0
                    ? "+"
                    : metres < 0.0
                        ? "-"
                        : "±";

            double absolute = Math.Abs(metres);

            if (absolute >= 1000.0)
            {
                return sign +
                       (absolute / 1000.0)
                           .ToString("F3") +
                       " km";
            }

            return sign +
                   absolute.ToString("F1") +
                   " m";
        }

        internal static string Angle(
            double degrees,
            int decimalPlaces)
        {
            return double.IsNaN(degrees)
                ? "—"
                : degrees.ToString(
                      "F" +
                      decimalPlaces) +
                  "°";
        }

        internal static string Velocity(double metresPerSecond)
        {
            return double.IsNaN(metresPerSecond)
                ? "—"
                : metresPerSecond.ToString("F2") +
                  " m/s";
        }

        internal static string Acceleration(
            double metresPerSecondSquared)
        {
            return double.IsNaN(metresPerSecondSquared)
                ? "—"
                : metresPerSecondSquared.ToString("F3") +
                  " m/s²";
        }

        internal static string Time(double seconds)
        {
            if (double.IsNaN(seconds) ||
                double.IsInfinity(seconds) ||
                seconds < 0.0)
            {
                return "—";
            }

            if (seconds < 60.0)
            {
                return seconds.ToString("F1") +
                       " s";
            }

            int totalSeconds =
                (int)Math.Round(seconds);

            int minutes =
                totalSeconds / 60;

            int remainder =
                totalSeconds % 60;

            return minutes +
                   "m " +
                   remainder.ToString("00") +
                   "s";
        }
    }
}
