// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

using System;
using UnityEngine;

namespace PlaneMatchKER.Core
{
    internal static class PlaneMatchProcessor
    {
        private const double PredictionSeconds = 20.0;
        private const double CaptureNaturalPeriodSeconds = 55.0;
        private const double MaximumSuggestedBankDegrees = 20.0;
        private const double MinimumRightNormalProjection = 0.20;
        private const double MinimumFlightDirectorDynamicPressureKpa = 0.05;
        private const double MinimumFlightDirectorHorizontalSpeed = 25.0;

        private static readonly PlaneMatchSolution solution =
            new PlaneMatchSolution();

        private static int lastFrameCount = -1;

        internal static PlaneMatchSolution Solution
        {
            get { return solution; }
        }

        internal static bool HasValidTarget
        {
            get
            {
                Update();
                return solution.Valid;
            }
        }

        internal static void Update()
        {
            // KER can ask the same readout to update more than once per frame.
            // Frame-based caching still responds to target changes while paused.
            if (Time.frameCount == lastFrameCount)
            {
                return;
            }

            lastFrameCount = Time.frameCount;
            ResetSolution();

            Vessel vessel = FlightGlobals.ActiveVessel;

            if (vessel == null)
            {
                Invalidate("No active vessel.");
                return;
            }

            if (FlightGlobals.fetch == null ||
                FlightGlobals.fetch.VesselTarget == null)
            {
                Invalidate("Select an orbiting vessel as target.");
                return;
            }

            Vessel target =
                FlightGlobals.fetch.VesselTarget.GetVessel();

            if (target == null)
            {
                Invalidate("Selected target is not a vessel.");
                return;
            }

            solution.TargetName = target.vesselName;

            if (ReferenceEquals(vessel, target))
            {
                Invalidate("The active vessel cannot target itself.");
                return;
            }

            if (target.LandedOrSplashed ||
                target.situation == Vessel.Situations.PRELAUNCH)
            {
                Invalidate("Target must be an airborne or orbiting vessel.");
                return;
            }

            if (vessel.mainBody == null ||
                target.mainBody == null ||
                vessel.mainBody != target.mainBody)
            {
                Invalidate("Target must share the same reference body.");
                return;
            }

            if (vessel.orbit == null ||
                target.orbit == null)
            {
                Invalidate("Active or target orbit is unavailable.");
                return;
            }

            try
            {
                CalculateSolution(
                    vessel,
                    target,
                    Planetarium.GetUniversalTime());
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PlaneMatchKER] calculation failed: " +
                    exception);

                Invalidate("Calculation failed; inspect KSP.log.");
            }
        }

        private static void CalculateSolution(
            Vessel vessel,
            Vessel target,
            double universalTime)
        {
            Vector3d craftPosition;
            Vector3d craftVelocity;
            Vector3d targetPosition;
            Vector3d targetVelocity;

            if (!PlaneGeometry.TryGetWorldState(
                    vessel.orbit,
                    universalTime,
                    out craftPosition,
                    out craftVelocity) ||
                !PlaneGeometry.TryGetWorldState(
                    target.orbit,
                    universalTime,
                    out targetPosition,
                    out targetVelocity))
            {
                Invalidate("An orbital state vector is unavailable.");
                return;
            }

            Vector3d craftNormal;
            Vector3d targetNormal;

            if (!PlaneGeometry.TryOrbitNormal(
                    craftPosition,
                    craftVelocity,
                    out craftNormal) ||
                !PlaneGeometry.TryOrbitNormal(
                    targetPosition,
                    targetVelocity,
                    out targetNormal))
            {
                Invalidate("An orbital-plane normal is degenerate.");
                return;
            }

            Vector3d radialUnit;

            if (!PlaneGeometry.TryNormalize(
                    craftPosition,
                    out radialUnit))
            {
                Invalidate("Craft radial direction is unavailable.");
                return;
            }

            solution.Valid = true;
            solution.Landed = vessel.LandedOrSplashed;
            solution.Status = "Valid target-plane solution.";

            solution.CraftInclinationDegrees =
                vessel.orbit.inclination;

            solution.CraftLanDegrees =
                vessel.orbit.LAN;

            solution.TargetInclinationDegrees =
                target.orbit.inclination;

            solution.TargetLanDegrees =
                target.orbit.LAN;

            solution.TrajectoryRelativeInclinationDegrees =
                PlaneGeometry.AngleDegrees(
                    craftNormal,
                    targetNormal);

            solution.KerStyleRelativeInclinationDegrees =
                solution.Landed
                    ? target.orbit.inclination
                    : solution.TrajectoryRelativeInclinationDegrees;

            double radius = craftPosition.magnitude;

            double signedPlaneSine =
                PlaneGeometry.Clamp(
                    Vector3d.Dot(radialUnit, targetNormal),
                    -1.0,
                    1.0);

            solution.PlaneAngleDegrees =
                Math.Asin(signedPlaneSine) *
                PlaneGeometry.DegreesPerRadian;

            solution.PlaneOffsetMetres =
                Vector3d.Dot(craftPosition, targetNormal);

            solution.NormalVelocityMetresPerSecond =
                Vector3d.Dot(craftVelocity, targetNormal);

            solution.NormalRateDegreesPerSecond =
                solution.NormalVelocityMetresPerSecond /
                radius *
                PlaneGeometry.DegreesPerRadian;

            solution.PredictedPlaneAngleDegrees =
                solution.PlaneAngleDegrees +
                solution.NormalRateDegreesPerSecond *
                PredictionSeconds;

            if (Math.Abs(
                    solution.NormalVelocityMetresPerSecond) >
                0.01)
            {
                double crossingSeconds =
                    -solution.PlaneOffsetMetres /
                    solution.NormalVelocityMetresPerSecond;

                if (crossingSeconds >= 0.0)
                {
                    solution.LinearPlaneCrossingSeconds =
                        crossingSeconds;
                }
            }

            Vector3d horizontalVelocity =
                PlaneGeometry.Reject(
                    craftVelocity,
                    radialUnit);

            solution.HorizontalInertialSpeed =
                horizontalVelocity.magnitude;

            solution.DynamicPressureKpa =
                vessel.dynamicPressurekPa;

            solution.PlaneSide =
                FormatPlaneSide(
                    solution.PlaneAngleDegrees);

            solution.NormalMotion =
                FormatNormalMotion(
                    solution.NormalVelocityMetresPerSecond);

            CalculateFlightDirector(
                vessel,
                radialUnit,
                targetNormal,
                horizontalVelocity,
                radius);
        }

        private static void CalculateFlightDirector(
            Vessel vessel,
            Vector3d radialUnit,
            Vector3d targetNormal,
            Vector3d horizontalVelocity,
            double radius)
        {
            if (solution.Landed)
            {
                InhibitFlightDirector(
                    "INHIBITED",
                    "LANDED");
                return;
            }

            if (solution.DynamicPressureKpa <
                MinimumFlightDirectorDynamicPressureKpa)
            {
                InhibitFlightDirector(
                    "N/A — VACUUM",
                    "ATMOSPHERIC CUE INACTIVE");
                return;
            }

            if (solution.HorizontalInertialSpeed <
                MinimumFlightDirectorHorizontalSpeed)
            {
                InhibitFlightDirector(
                    "N/A — LOW SPEED",
                    "INSUFFICIENT TRACK DEFINITION");
                return;
            }

            Vector3d currentTrack;

            if (!PlaneGeometry.TryNormalize(
                    horizontalVelocity,
                    out currentTrack))
            {
                InhibitFlightDirector(
                    "UNAVAILABLE",
                    "TRACK DIRECTION UNAVAILABLE");
                return;
            }

            Vector3d horizontalRight =
                Vector3d.Cross(
                    radialUnit,
                    currentTrack);

            if (!PlaneGeometry.TryNormalize(
                    horizontalRight,
                    out horizontalRight))
            {
                InhibitFlightDirector(
                    "UNAVAILABLE",
                    "LOCAL RIGHT DIRECTION UNAVAILABLE");
                return;
            }

            Vector3d horizontalTargetNormal =
                PlaneGeometry.Reject(
                    targetNormal,
                    radialUnit);

            if (!PlaneGeometry.TryNormalize(
                    horizontalTargetNormal,
                    out horizontalTargetNormal))
            {
                InhibitFlightDirector(
                    "UNAVAILABLE",
                    "TARGET-NORMAL DIRECTION UNAVAILABLE");
                return;
            }

            double rightToNormal =
                Vector3d.Dot(
                    horizontalRight,
                    horizontalTargetNormal);

            solution.RightToTargetNormalProjection =
                rightToNormal;

            if (Math.Abs(rightToNormal) <
                MinimumRightNormalProjection)
            {
                InhibitFlightDirector(
                    "WEAK GEOMETRY",
                    "LOW BANK AUTHORITY");
                return;
            }

            double omega =
                1.0 /
                CaptureNaturalPeriodSeconds;

            double desiredNormalAcceleration =
                -(omega * omega) *
                    solution.PlaneOffsetMetres -
                2.0 * omega *
                    solution.NormalVelocityMetresPerSecond;

            double localGravity =
                vessel.mainBody.gravParameter /
                (radius * radius);

            double maximumNormalAcceleration =
                localGravity *
                Math.Tan(
                    MaximumSuggestedBankDegrees *
                    PlaneGeometry.RadiansPerDegree) *
                Math.Abs(rightToNormal);

            desiredNormalAcceleration =
                PlaneGeometry.Clamp(
                    desiredNormalAcceleration,
                    -maximumNormalAcceleration,
                    maximumNormalAcceleration);

            solution.DesiredNormalAccelerationMetresPerSecondSquared =
                desiredNormalAcceleration;

            double desiredRightAcceleration =
                desiredNormalAcceleration /
                rightToNormal;

            solution.DesiredBankDegrees =
                PlaneGeometry.Clamp(
                    Math.Atan2(
                        desiredRightAcceleration,
                        localGravity) *
                    PlaneGeometry.DegreesPerRadian,
                    -MaximumSuggestedBankDegrees,
                    MaximumSuggestedBankDegrees);

            solution.BankCue =
                FormatBankCue(
                    solution.DesiredBankDegrees);

            bool movingTowardPlane =
                solution.PlaneOffsetMetres *
                solution.NormalVelocityMetresPerSecond <
                0.0;

            double requiredStoppingAcceleration = 0.0;

            if (movingTowardPlane &&
                Math.Abs(solution.PlaneOffsetMetres) > 1.0)
            {
                requiredStoppingAcceleration =
                    solution.NormalVelocityMetresPerSecond *
                    solution.NormalVelocityMetresPerSecond /
                    (2.0 *
                     Math.Abs(solution.PlaneOffsetMetres));
            }

            solution.RequiredStoppingAccelerationMetresPerSecondSquared =
                requiredStoppingAcceleration;

            solution.RequiredStoppingBankDegrees =
                Math.Atan2(
                    requiredStoppingAcceleration /
                    Math.Abs(rightToNormal),
                    localGravity) *
                PlaneGeometry.DegreesPerRadian;

            double projectedStoppingDistance = 0.0;

            if (maximumNormalAcceleration > 1.0e-6)
            {
                projectedStoppingDistance =
                    solution.NormalVelocityMetresPerSecond *
                    Math.Abs(
                        solution.NormalVelocityMetresPerSecond) /
                    (2.0 *
                     maximumNormalAcceleration);
            }

            solution.ProjectedStopOffsetMetres =
                solution.PlaneOffsetMetres +
                projectedStoppingDistance;

            solution.CaptureFeasible =
                !movingTowardPlane ||
                requiredStoppingAcceleration <=
                    maximumNormalAcceleration;

            solution.CaptureStatus =
                solution.CaptureFeasible
                    ? "FEASIBLE ≤20°"
                    : "LATE >20°";

            solution.FlightDirectorAvailable = true;
        }

        private static void InhibitFlightDirector(
            string bankCue,
            string captureStatus)
        {
            solution.FlightDirectorAvailable = false;
            solution.BankCue = bankCue;
            solution.CaptureStatus = captureStatus;
            solution.DesiredNormalAccelerationMetresPerSecondSquared =
                double.NaN;
            solution.DesiredBankDegrees = double.NaN;
            solution.RequiredStoppingAccelerationMetresPerSecondSquared =
                double.NaN;
            solution.RequiredStoppingBankDegrees = double.NaN;
            solution.ProjectedStopOffsetMetres = double.NaN;
            solution.RightToTargetNormalProjection = double.NaN;
        }

        private static void ResetSolution()
        {
            solution.Valid = false;
            solution.Landed = false;
            solution.FlightDirectorAvailable = false;
            solution.CaptureFeasible = false;

            solution.Status = "No solution.";
            solution.TargetName = "None";
            solution.PlaneSide = "—";
            solution.NormalMotion = "—";
            solution.BankCue = "INHIBITED";
            solution.CaptureStatus = "UNKNOWN";

            solution.TargetInclinationDegrees = double.NaN;
            solution.TargetLanDegrees = double.NaN;
            solution.CraftInclinationDegrees = double.NaN;
            solution.CraftLanDegrees = double.NaN;
            solution.TrajectoryRelativeInclinationDegrees =
                double.NaN;
            solution.KerStyleRelativeInclinationDegrees =
                double.NaN;
            solution.PlaneAngleDegrees = double.NaN;
            solution.PlaneOffsetMetres = double.NaN;
            solution.NormalVelocityMetresPerSecond =
                double.NaN;
            solution.NormalRateDegreesPerSecond =
                double.NaN;
            solution.PredictedPlaneAngleDegrees =
                double.NaN;
            solution.LinearPlaneCrossingSeconds =
                double.NaN;
            solution.HorizontalInertialSpeed =
                double.NaN;
            solution.DynamicPressureKpa =
                double.NaN;
            solution.DesiredNormalAccelerationMetresPerSecondSquared =
                double.NaN;
            solution.DesiredBankDegrees = double.NaN;
            solution.RequiredStoppingAccelerationMetresPerSecondSquared =
                double.NaN;
            solution.RequiredStoppingBankDegrees =
                double.NaN;
            solution.ProjectedStopOffsetMetres =
                double.NaN;
            solution.RightToTargetNormalProjection =
                double.NaN;
        }

        private static void Invalidate(string message)
        {
            solution.Status = message;
            solution.Valid = false;
        }

        private static string FormatPlaneSide(
            double planeAngleDegrees)
        {
            if (double.IsNaN(planeAngleDegrees))
            {
                return "—";
            }

            if (Math.Abs(planeAngleDegrees) < 0.001)
            {
                return "ON TARGET PLANE";
            }

            return planeAngleDegrees > 0.0
                ? "NORMAL SIDE (+)"
                : "ANTINORMAL SIDE (-)";
        }

        private static string FormatNormalMotion(
            double normalVelocity)
        {
            if (double.IsNaN(normalVelocity))
            {
                return "—";
            }

            if (Math.Abs(normalVelocity) < 0.05)
            {
                return "PARALLEL";
            }

            return normalVelocity > 0.0
                ? "TOWARD NORMAL (+)"
                : "TOWARD ANTINORMAL (-)";
        }

        private static string FormatBankCue(
            double bankDegrees)
        {
            if (double.IsNaN(bankDegrees))
            {
                return "INHIBITED";
            }

            if (Math.Abs(bankDegrees) < 0.5)
            {
                return "WINGS LEVEL";
            }

            return bankDegrees > 0.0
                ? "RIGHT " +
                  Math.Abs(bankDegrees).ToString("F1") +
                  "°"
                : "LEFT " +
                  Math.Abs(bankDegrees).ToString("F1") +
                  "°";
        }
    }
}
