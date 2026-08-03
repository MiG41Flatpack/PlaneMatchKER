// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 LaunchWindowKER contributors

namespace LaunchWindowKER.Core
{
    internal sealed class LaunchWindowSolution
    {
        internal bool Valid;
        internal string Status = "No solution.";
        internal string TargetName = "None";

        internal double CurrentSlantRangeMetres = double.NaN;
        internal double CurrentRangeRateMetresPerSecond = double.NaN;
        internal double CurrentRelativeSpeedMetresPerSecond = double.NaN;
        internal double CurrentElevationDegrees = double.NaN;
        internal double CurrentAzimuthDegrees = double.NaN;
        internal double CurrentSurfaceDistanceMetres = double.NaN;
        internal string TargetMotion = "—";

        internal bool InsideProximityReference;
        internal double ProximityMarginMetres = double.NaN;

        internal string ForecastReferenceMode = "—";
        internal double ReferenceLatitudeDegrees = double.NaN;
        internal double ReferenceLongitudeDegrees = double.NaN;

        internal double NextRiseUT = double.NaN;
        internal double NextMaximumUT = double.NaN;
        internal double NextMaximumElevationDegrees = double.NaN;
        internal double NextMaximumSurfaceDistanceMetres = double.NaN;
        internal double NextSetUT = double.NaN;

        internal double NextPlaneWindowUT = double.NaN;
        internal string PlaneBranch = "—";
        internal double LaunchAzimuthDegrees = double.NaN;
        internal double TargetElevationAtPlaneDegrees = double.NaN;
        internal double TargetRangeAtPlaneMetres = double.NaN;
        internal string TargetMotionAtPlane = "—";
        internal string NextWindowHeuristic = "—";

        internal double SearchSpanSeconds = double.NaN;
    }
}
