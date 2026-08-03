// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 LaunchWindowKER contributors

using System;

namespace LaunchWindowKER.Core
{
    internal static class LaunchWindowFormatter
    {
        internal static string Distance(double metres)
        {
            if (!WindowGeometry.IsFinite(metres))
            {
                return "—";
            }

            double absolute = Math.Abs(metres);

            if (absolute >= 1000000.0)
            {
                return
                    (absolute / 1000000.0)
                        .ToString("F3") +
                    " Mm";
            }

            if (absolute >= 1000.0)
            {
                return
                    (absolute / 1000.0)
                        .ToString("F3") +
                    " km";
            }

            return
                absolute.ToString("F1") +
                " m";
        }

        internal static string SignedDistance(double metres)
        {
            if (!WindowGeometry.IsFinite(metres))
            {
                return "—";
            }

            string sign =
                metres > 0.0
                    ? "+"
                    : metres < 0.0
                        ? "-"
                        : "±";

            return sign +
                   Distance(metres);
        }

        internal static string SignedVelocity(
            double metresPerSecond)
        {
            if (!WindowGeometry.IsFinite(
                    metresPerSecond))
            {
                return "—";
            }

            string sign =
                metresPerSecond > 0.0
                    ? "+"
                    : metresPerSecond < 0.0
                        ? "-"
                        : "±";

            return
                sign +
                Math.Abs(metresPerSecond)
                    .ToString("F2") +
                " m/s";
        }

        internal static string Velocity(
            double metresPerSecond)
        {
            if (!WindowGeometry.IsFinite(
                    metresPerSecond))
            {
                return "—";
            }

            return
                Math.Abs(metresPerSecond)
                    .ToString("F2") +
                " m/s";
        }

        internal static string Angle(
            double degrees,
            int decimals)
        {
            if (!WindowGeometry.IsFinite(degrees))
            {
                return "—";
            }

            return
                degrees.ToString(
                    "F" + decimals) +
                "°";
        }

        internal static string Azimuth(double degrees)
        {
            if (!WindowGeometry.IsFinite(degrees))
            {
                return "—";
            }

            return
                WindowGeometry.Wrap360(degrees)
                    .ToString("F1") +
                "°";
        }

        internal static string Countdown(
            double eventUT,
            double nowUT)
        {
            if (!WindowGeometry.IsFinite(eventUT))
            {
                return "—";
            }

            double seconds =
                eventUT -
                nowUT;

            if (Math.Abs(seconds) < 0.5)
            {
                return "NOW";
            }

            if (seconds < 0.0)
            {
                return "PASSED";
            }

            return
                "T-" +
                Duration(seconds);
        }

        internal static string Duration(double seconds)
        {
            if (!WindowGeometry.IsFinite(seconds) ||
                seconds < 0.0)
            {
                return "—";
            }

            long totalSeconds =
                (long)Math.Round(seconds);

            long days =
                totalSeconds / 86400;

            totalSeconds %= 86400;

            long hours =
                totalSeconds / 3600;

            totalSeconds %= 3600;

            long minutes =
                totalSeconds / 60;

            long remainingSeconds =
                totalSeconds % 60;

            if (days > 0)
            {
                return
                    days +
                    "d " +
                    hours.ToString("00") +
                    "h " +
                    minutes.ToString("00") +
                    "m";
            }

            if (hours > 0)
            {
                return
                    hours +
                    "h " +
                    minutes.ToString("00") +
                    "m " +
                    remainingSeconds.ToString("00") +
                    "s";
            }

            if (minutes > 0)
            {
                return
                    minutes +
                    "m " +
                    remainingSeconds.ToString("00") +
                    "s";
            }

            return
                remainingSeconds +
                "s";
        }

        internal static string Latitude(double degrees)
        {
            if (!WindowGeometry.IsFinite(degrees))
            {
                return "—";
            }

            return
                Math.Abs(degrees).ToString("F3") +
                "° " +
                (degrees >= 0.0 ? "N" : "S");
        }

        internal static string Longitude(double degrees)
        {
            if (!WindowGeometry.IsFinite(degrees))
            {
                return "—";
            }

            double wrapped =
                ((degrees + 180.0) % 360.0 + 360.0) %
                360.0 -
                180.0;

            return
                Math.Abs(wrapped).ToString("F3") +
                "° " +
                (wrapped >= 0.0 ? "E" : "W");
        }
    }
}
