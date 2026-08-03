// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

namespace PlaneMatchKER.Core
{
    internal sealed class PlaneMatchSolution
    {
        internal bool Valid;
        internal bool Landed;
        internal bool FlightDirectorAvailable;
        internal bool CaptureFeasible;

        internal string Status = "No solution.";
        internal string TargetName = "None";
        internal string PlaneSide = "—";
        internal string NormalMotion = "—";
        internal string BankCue = "INHIBITED";
        internal string CaptureStatus = "UNKNOWN";

        internal double TargetInclinationDegrees = double.NaN;
        internal double TargetLanDegrees = double.NaN;
        internal double CraftInclinationDegrees = double.NaN;
        internal double CraftLanDegrees = double.NaN;

        internal double TrajectoryRelativeInclinationDegrees = double.NaN;
        internal double KerStyleRelativeInclinationDegrees = double.NaN;

        internal double PlaneAngleDegrees = double.NaN;
        internal double PlaneOffsetMetres = double.NaN;
        internal double NormalVelocityMetresPerSecond = double.NaN;
        internal double NormalRateDegreesPerSecond = double.NaN;
        internal double PredictedPlaneAngleDegrees = double.NaN;
        internal double LinearPlaneCrossingSeconds = double.NaN;

        internal double HorizontalInertialSpeed = double.NaN;
        internal double DynamicPressureKpa = double.NaN;
        internal double DesiredNormalAccelerationMetresPerSecondSquared =
            double.NaN;
        internal double DesiredBankDegrees = double.NaN;
        internal double RequiredStoppingAccelerationMetresPerSecondSquared =
            double.NaN;
        internal double RequiredStoppingBankDegrees = double.NaN;
        internal double ProjectedStopOffsetMetres = double.NaN;
        internal double RightToTargetNormalProjection = double.NaN;
    }
}
