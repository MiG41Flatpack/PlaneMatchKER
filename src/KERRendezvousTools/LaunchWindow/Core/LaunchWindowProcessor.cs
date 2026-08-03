// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 LaunchWindowKER contributors

using System;
using UnityEngine;

namespace LaunchWindowKER.Core
{
    internal static class LaunchWindowProcessor
    {
        private const double ForecastRefreshSeconds = 2.0;
        private const double RootToleranceSeconds = 0.05;
        private const double PlaneNowToleranceSine = 0.0001745329;
        private const int MaximumPassSamples = 12000;
        private const int PlaneSamplesPerRotation = 1440;

        private static readonly LaunchWindowSolution solution =
            new LaunchWindowSolution();

        private static readonly GoodWindowSearch goodWindowSearch =
            new GoodWindowSearch();

        private static int lastFrameCount = -1;
        private static double lastForecastUT = double.NaN;
        private static Guid lastTargetId = Guid.Empty;

        private static Guid referenceVesselId = Guid.Empty;
        private static CelestialBody referenceBody;
        private static Vector3d referenceSeaLevelAtEpoch = Vector3d.zero;
        private static double referenceEpochUT = double.NaN;
        private static bool referenceEstablished;
        private static bool referenceCapturedOnSurface;

        internal static LaunchWindowSolution Solution
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
            if (Time.frameCount == lastFrameCount)
            {
                return;
            }

            lastFrameCount = Time.frameCount;

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

            if (ReferenceEquals(vessel, target))
            {
                Invalidate("The active vessel cannot target itself.");
                return;
            }

            if (vessel.mainBody == null ||
                target.mainBody == null ||
                vessel.mainBody != target.mainBody)
            {
                Invalidate("Target must orbit the same body.");
                return;
            }

            if (target.orbit == null ||
                target.situation != Vessel.Situations.ORBITING)
            {
                Invalidate("Target must be in a bound orbit.");
                return;
            }

            if (!WindowGeometry.IsFinite(target.orbit.period) ||
                target.orbit.period <= 1.0)
            {
                Invalidate("Target orbital period is unavailable.");
                return;
            }

            try
            {
                Calculate(
                    vessel,
                    target,
                    vessel.mainBody,
                    Planetarium.GetUniversalTime());
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[KERRendezvousTools/WIND] calculation failed: " +
                    exception);

                Invalidate(
                    "Calculation failed; inspect KSP.log.");
            }
        }

        private static void Calculate(
            Vessel vessel,
            Vessel target,
            CelestialBody body,
            double nowUT)
        {
            Vector3d vesselRelative;
            Vector3d targetPosition;
            Vector3d targetVelocity;

            if (!WindowGeometry.TryGetVesselRelativePosition(
                    vessel,
                    body,
                    out vesselRelative) ||
                !WindowGeometry.TryGetOrbitState(
                    target.orbit,
                    nowUT,
                    out targetPosition,
                    out targetVelocity))
            {
                Invalidate(
                    "A current state vector is unavailable.");
                return;
            }

            Vector3d currentRadialUnit;

            if (!WindowGeometry.TryNormalize(
                    vesselRelative,
                    out currentRadialUnit))
            {
                Invalidate(
                    "The active-vessel radial direction is unavailable.");
                return;
            }

            Vector3d currentSeaLevelSite =
                currentRadialUnit *
                body.Radius;

            Vector3d rotationAxis;

            if (!WindowGeometry.TryRotationAxis(
                    body,
                    out rotationAxis))
            {
                Invalidate(
                    "Body rotation axis is unavailable.");
                return;
            }

            Vector3d orbitNormal;

            if (!WindowGeometry.TryOrbitNormal(
                    targetPosition,
                    targetVelocity,
                    out orbitNormal))
            {
                Invalidate(
                    "Target orbital plane is unavailable.");
                return;
            }

            UpdateForecastReference(
                vessel,
                body,
                currentSeaLevelSite,
                nowUT);

            Vector3d forecastSiteNow =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSeaLevelAtEpoch,
                    body,
                    referenceEpochUT,
                    nowUT);

            solution.Valid = true;
            solution.Status =
                "Generic pass and plane timing.";
            solution.TargetName =
                target.vesselName;

            Vector3d currentLineOfSight =
                targetPosition -
                vesselRelative;

            solution.CurrentSlantRangeMetres =
                currentLineOfSight.magnitude;

            Vector3d vesselVelocity =
                vessel.obt_velocity;

            Vector3d relativeVelocity =
                targetVelocity -
                vesselVelocity;

            solution.CurrentRelativeSpeedMetresPerSecond =
                relativeVelocity.magnitude;

            Vector3d lineOfSightUnit;

            if (WindowGeometry.TryNormalize(
                    currentLineOfSight,
                    out lineOfSightUnit))
            {
                solution.CurrentRangeRateMetresPerSecond =
                    Vector3d.Dot(
                        relativeVelocity,
                        lineOfSightUnit);
            }
            else
            {
                solution.CurrentRangeRateMetresPerSecond =
                    double.NaN;
            }

            solution.CurrentElevationDegrees =
                WindowGeometry.ElevationDegrees(
                    currentSeaLevelSite,
                    targetPosition);

            solution.CurrentAzimuthDegrees =
                WindowGeometry.AzimuthDegrees(
                    currentSeaLevelSite,
                    targetPosition,
                    rotationAxis);

            solution.CurrentSurfaceDistanceMetres =
                WindowGeometry.SurfaceDistanceMetres(
                    currentSeaLevelSite,
                    targetPosition,
                    body.Radius);

            solution.TargetMotion =
                MotionAt(
                    target.orbit,
                    body,
                    currentSeaLevelSite,
                    nowUT,
                    nowUT);

            solution.InsideProximityReference =
                solution.CurrentSlantRangeMetres <=
                ModInfo.ProximityThresholdMetres;

            solution.ProximityMarginMetres =
                solution.CurrentSlantRangeMetres -
                ModInfo.ProximityThresholdMetres;

            Vector3d referenceWorldPosition =
                body.position +
                forecastSiteNow;

            solution.ReferenceLatitudeDegrees =
                body.GetLatitude(
                    referenceWorldPosition);

            solution.ReferenceLongitudeDegrees =
                body.GetLongitude(
                    referenceWorldPosition);

            bool targetChanged =
                target.id != lastTargetId;

            bool forecastExpired =
                !WindowGeometry.IsFinite(lastForecastUT) ||
                nowUT - lastForecastUT >=
                    ForecastRefreshSeconds;

            bool eventPassed =
                EventPassed(solution.NextRiseUT, nowUT) ||
                EventPassed(solution.NextMaximumUT, nowUT) ||
                EventPassed(solution.NextSetUT, nowUT) ||
                EventPassed(solution.NextPlaneWindowUT, nowUT);

            if (targetChanged ||
                forecastExpired ||
                eventPassed)
            {
                SolveForecast(
                    target.orbit,
                    body,
                    referenceSeaLevelAtEpoch,
                    referenceEpochUT,
                    orbitNormal,
                    rotationAxis,
                    nowUT);

                lastForecastUT = nowUT;
                lastTargetId = target.id;
            }

            goodWindowSearch.EnsureConfigured(
                vessel.id,
                target.id,
                target.orbit,
                body,
                referenceSeaLevelAtEpoch,
                referenceEpochUT,
                orbitNormal,
                rotationAxis,
                nowUT,
                solution.ReferenceLatitudeDegrees,
                solution.ReferenceLongitudeDegrees);

            goodWindowSearch.Step(
                target.orbit,
                nowUT);

            goodWindowSearch.PopulateSolution(
                solution,
                nowUT);
        }

        private static void UpdateForecastReference(
            Vessel vessel,
            CelestialBody body,
            Vector3d currentSeaLevelSite,
            double nowUT)
        {
            bool vesselChanged =
                vessel.id != referenceVesselId ||
                body != referenceBody;

            if (vesselChanged)
            {
                ResetReference();

                referenceVesselId = vessel.id;
                referenceBody = body;
            }

            bool onSurface =
                vessel.situation ==
                    Vessel.Situations.PRELAUNCH ||
                vessel.LandedOrSplashed;

            if (!referenceEstablished)
            {
                referenceSeaLevelAtEpoch =
                    currentSeaLevelSite;
                referenceEpochUT = nowUT;
                referenceEstablished = true;
                referenceCapturedOnSurface = onSurface;

                solution.ForecastReferenceMode =
                    onSurface
                        ? "CURRENT SURFACE SITE"
                        : "IN-FLIGHT SNAPSHOT";

                lastForecastUT = double.NaN;
                return;
            }

            if (onSurface)
            {
                referenceSeaLevelAtEpoch =
                    currentSeaLevelSite;
                referenceEpochUT = nowUT;
                referenceCapturedOnSurface = true;

                solution.ForecastReferenceMode =
                    "CURRENT SURFACE SITE";

                return;
            }

            solution.ForecastReferenceMode =
                referenceCapturedOnSurface
                    ? "LAUNCH SITE LOCKED"
                    : "IN-FLIGHT SNAPSHOT";
        }

        private static void ResetReference()
        {
            referenceVesselId = Guid.Empty;
            referenceBody = null;
            referenceSeaLevelAtEpoch = Vector3d.zero;
            referenceEpochUT = double.NaN;
            referenceEstablished = false;
            referenceCapturedOnSurface = false;

            lastForecastUT = double.NaN;
            lastTargetId = Guid.Empty;
        }

        private static bool EventPassed(
            double eventUT,
            double nowUT)
        {
            return
                WindowGeometry.IsFinite(eventUT) &&
                eventUT < nowUT - 0.5;
        }

        private static void SolveForecast(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            Vector3d targetOrbitNormal,
            Vector3d rotationAxis,
            double nowUT)
        {
            ResetForecast();

            double targetPeriod =
                targetOrbit.period;

            double rotationPeriod =
                body.rotates &&
                WindowGeometry.IsFinite(
                    body.rotationPeriod) &&
                Math.Abs(body.rotationPeriod) > 1.0
                    ? Math.Abs(body.rotationPeriod)
                    : targetPeriod * 12.0;

            double searchSpan =
                Math.Max(
                    targetPeriod * 8.0,
                    rotationPeriod * 1.05);

            searchSpan =
                WindowGeometry.Clamp(
                    searchSpan,
                    targetPeriod * 2.0,
                    30.0 * 86400.0);

            solution.SearchSpanSeconds =
                searchSpan;

            SolveNextVisiblePass(
                targetOrbit,
                body,
                referenceSiteAtEpoch,
                referenceEpoch,
                nowUT,
                targetPeriod,
                searchSpan);

            SolvePlaneWindow(
                targetOrbit,
                body,
                referenceSiteAtEpoch,
                referenceEpoch,
                targetOrbitNormal,
                rotationAxis,
                nowUT,
                rotationPeriod);
        }

        private static void SolveNextVisiblePass(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double nowUT,
            double targetPeriod,
            double searchSpan)
        {
            int desiredSamples =
                (int)Math.Ceiling(
                    searchSpan /
                    Math.Max(
                        1.0,
                        targetPeriod / 72.0));

            int sampleCount =
                Math.Max(
                    720,
                    Math.Min(
                        MaximumPassSamples,
                        desiredSamples));

            double step =
                searchSpan /
                sampleCount;

            double previousUT = nowUT;
            double previousElevation =
                ElevationAt(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    nowUT);

            if (!WindowGeometry.IsFinite(
                    previousElevation))
            {
                return;
            }

            bool passActive =
                previousElevation >= 0.0;

            double passStartUT =
                passActive
                    ? nowUT
                    : double.NaN;

            if (passActive)
            {
                solution.NextRiseUT = nowUT;
            }

            for (int index = 1;
                 index <= sampleCount;
                 index++)
            {
                double currentUT =
                    nowUT +
                    step * index;

                double currentElevation =
                    ElevationAt(
                        targetOrbit,
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        currentUT);

                if (!WindowGeometry.IsFinite(
                        currentElevation))
                {
                    continue;
                }

                if (!passActive &&
                    previousElevation < 0.0 &&
                    currentElevation >= 0.0)
                {
                    double riseUT =
                        RefineHorizonRoot(
                            targetOrbit,
                            body,
                            referenceSiteAtEpoch,
                            referenceEpoch,
                            previousUT,
                            currentUT);

                    solution.NextRiseUT = riseUT;
                    passStartUT = riseUT;
                    passActive = true;
                }

                if (passActive &&
                    previousElevation > 0.0 &&
                    currentElevation <= 0.0)
                {
                    double setUT =
                        RefineHorizonRoot(
                            targetOrbit,
                            body,
                            referenceSiteAtEpoch,
                            referenceEpoch,
                            previousUT,
                            currentUT);

                    solution.NextSetUT = setUT;

                    if (WindowGeometry.IsFinite(
                            passStartUT))
                    {
                        PopulatePassMaximum(
                            targetOrbit,
                            body,
                            referenceSiteAtEpoch,
                            referenceEpoch,
                            passStartUT,
                            setUT);
                    }

                    return;
                }

                previousUT = currentUT;
                previousElevation =
                    currentElevation;
            }
        }

        private static void PopulatePassMaximum(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double passStartUT,
            double passEndUT)
        {
            double maximumUT =
                RefineMaximum(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    passStartUT,
                    passEndUT);

            solution.NextMaximumUT =
                maximumUT;

            Vector3d maximumTarget =
                TargetPositionAt(
                    targetOrbit,
                    maximumUT);

            Vector3d maximumSite =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    maximumUT);

            solution.NextMaximumElevationDegrees =
                WindowGeometry.ElevationDegrees(
                    maximumSite,
                    maximumTarget);

            solution.NextMaximumSurfaceDistanceMetres =
                WindowGeometry.SurfaceDistanceMetres(
                    maximumSite,
                    maximumTarget,
                    body.Radius);
        }

        private static void SolvePlaneWindow(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            Vector3d targetOrbitNormal,
            Vector3d rotationAxis,
            double nowUT,
            double rotationPeriod)
        {
            Vector3d siteNow =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    nowUT);

            Vector3d siteUnit;

            if (!WindowGeometry.TryNormalize(
                    siteNow,
                    out siteUnit))
            {
                return;
            }

            double atNow =
                Vector3d.Dot(
                    siteUnit,
                    targetOrbitNormal);

            if (Math.Abs(atNow) <=
                PlaneNowToleranceSine)
            {
                solution.NextPlaneWindowUT =
                    nowUT;

                PopulatePlaneMetadata(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    targetOrbitNormal,
                    rotationAxis,
                    nowUT);

                return;
            }

            double span =
                body.rotates
                    ? rotationPeriod * 1.01
                    : targetOrbit.period;

            int samples =
                body.rotates
                    ? PlaneSamplesPerRotation
                    : 2;

            double step =
                span /
                samples;

            double previousUT = nowUT;
            double previousValue = atNow;

            for (int index = 1;
                 index <= samples;
                 index++)
            {
                double currentUT =
                    nowUT +
                    step * index;

                double currentValue =
                    PlaneFunction(
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        targetOrbitNormal,
                        currentUT);

                if (!WindowGeometry.IsFinite(
                        currentValue))
                {
                    continue;
                }

                if (previousValue == 0.0 ||
                    previousValue *
                        currentValue <= 0.0)
                {
                    double root =
                        RefinePlaneRoot(
                            body,
                            referenceSiteAtEpoch,
                            referenceEpoch,
                            targetOrbitNormal,
                            previousUT,
                            currentUT);

                    solution.NextPlaneWindowUT =
                        root;

                    PopulatePlaneMetadata(
                        targetOrbit,
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        targetOrbitNormal,
                        rotationAxis,
                        root);

                    return;
                }

                previousUT = currentUT;
                previousValue = currentValue;
            }

            solution.NextWindowHeuristic =
                "NO ACCESSIBLE PLANE CROSSING";
        }

        private static void PopulatePlaneMetadata(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            Vector3d targetOrbitNormal,
            Vector3d rotationAxis,
            double planeUT)
        {
            Vector3d site =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    planeUT);

            Vector3d target =
                TargetPositionAt(
                    targetOrbit,
                    planeUT);

            solution.PlaneBranch =
                WindowGeometry.PlaneBranch(
                    site,
                    targetOrbitNormal,
                    rotationAxis);

            solution.LaunchAzimuthDegrees =
                WindowGeometry.LaunchAzimuthDegrees(
                    site,
                    targetOrbitNormal,
                    rotationAxis);

            solution.TargetElevationAtPlaneDegrees =
                WindowGeometry.ElevationDegrees(
                    site,
                    target);

            solution.TargetRangeAtPlaneMetres =
                (target - site).magnitude;

            solution.TargetMotionAtPlane =
                MotionAt(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    planeUT);

            solution.NextWindowHeuristic =
                ClassifyHorizonHeuristic(
                    solution.TargetElevationAtPlaneDegrees,
                    solution.TargetMotionAtPlane);
        }

        private static string ClassifyHorizonHeuristic(
            double elevationDegrees,
            string motion)
        {
            if (!WindowGeometry.IsFinite(
                    elevationDegrees))
            {
                return "UNAVAILABLE";
            }

            if (motion != "RISING")
            {
                return "SETTING: POOR HEURISTIC";
            }

            if (Math.Abs(elevationDegrees) <= 5.0)
            {
                return "GOOD: RISING NEAR HORIZON";
            }

            if (elevationDegrees < -5.0)
            {
                return "EARLY: TARGET BELOW";
            }

            return "LATE: TARGET ALREADY HIGH";
        }

        private static double RefineHorizonRoot(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double lowerUT,
            double upperUT)
        {
            double lower =
                ElevationAt(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    lowerUT);

            for (int iteration = 0;
                 iteration < 48 &&
                 upperUT - lowerUT >
                    RootToleranceSeconds;
                 iteration++)
            {
                double middleUT =
                    0.5 *
                    (lowerUT + upperUT);

                double middle =
                    ElevationAt(
                        targetOrbit,
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        middleUT);

                if (lower == 0.0)
                {
                    return lowerUT;
                }

                if (lower * middle <= 0.0)
                {
                    upperUT = middleUT;
                }
                else
                {
                    lowerUT = middleUT;
                    lower = middle;
                }
            }

            return
                0.5 *
                (lowerUT + upperUT);
        }

        private static double RefinePlaneRoot(
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            Vector3d targetOrbitNormal,
            double lowerUT,
            double upperUT)
        {
            double lower =
                PlaneFunction(
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    targetOrbitNormal,
                    lowerUT);

            for (int iteration = 0;
                 iteration < 48 &&
                 upperUT - lowerUT >
                    RootToleranceSeconds;
                 iteration++)
            {
                double middleUT =
                    0.5 *
                    (lowerUT + upperUT);

                double middle =
                    PlaneFunction(
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        targetOrbitNormal,
                        middleUT);

                if (lower == 0.0)
                {
                    return lowerUT;
                }

                if (lower * middle <= 0.0)
                {
                    upperUT = middleUT;
                }
                else
                {
                    lowerUT = middleUT;
                    lower = middle;
                }
            }

            return
                0.5 *
                (lowerUT + upperUT);
        }

        private static double RefineMaximum(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double lowerUT,
            double upperUT)
        {
            for (int iteration = 0;
                 iteration < 42;
                 iteration++)
            {
                double first =
                    lowerUT +
                    (upperUT - lowerUT) /
                    3.0;

                double second =
                    upperUT -
                    (upperUT - lowerUT) /
                    3.0;

                double firstElevation =
                    ElevationAt(
                        targetOrbit,
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        first);

                double secondElevation =
                    ElevationAt(
                        targetOrbit,
                        body,
                        referenceSiteAtEpoch,
                        referenceEpoch,
                        second);

                if (firstElevation <
                    secondElevation)
                {
                    lowerUT = first;
                }
                else
                {
                    upperUT = second;
                }
            }

            return
                0.5 *
                (lowerUT + upperUT);
        }

        private static double PlaneFunction(
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            Vector3d targetOrbitNormal,
            double universalTime)
        {
            Vector3d site =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    universalTime);

            Vector3d siteUnit;

            if (!WindowGeometry.TryNormalize(
                    site,
                    out siteUnit))
            {
                return double.NaN;
            }

            return
                Vector3d.Dot(
                    siteUnit,
                    targetOrbitNormal);
        }

        private static double ElevationAt(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double universalTime)
        {
            Vector3d site =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    universalTime);

            Vector3d target =
                TargetPositionAt(
                    targetOrbit,
                    universalTime);

            return
                WindowGeometry.ElevationDegrees(
                    site,
                    target);
        }

        private static Vector3d TargetPositionAt(
            Orbit targetOrbit,
            double universalTime)
        {
            return
                WindowGeometry.OrbitVectorToWorld(
                    targetOrbit.getRelativePositionAtUT(
                        universalTime));
        }

        private static string MotionAt(
            Orbit targetOrbit,
            CelestialBody body,
            Vector3d referenceSiteAtEpoch,
            double referenceEpoch,
            double universalTime)
        {
            double before =
                ElevationAt(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    universalTime - 0.5);

            double after =
                ElevationAt(
                    targetOrbit,
                    body,
                    referenceSiteAtEpoch,
                    referenceEpoch,
                    universalTime + 0.5);

            if (!WindowGeometry.IsFinite(before) ||
                !WindowGeometry.IsFinite(after))
            {
                return "—";
            }

            double rate =
                after -
                before;

            if (Math.Abs(rate) < 1.0e-4)
            {
                return "CULMINATING";
            }

            return rate > 0.0
                ? "RISING"
                : "SETTING";
        }

        private static void ResetForecast()
        {
            solution.NextRiseUT = double.NaN;
            solution.NextMaximumUT = double.NaN;
            solution.NextMaximumElevationDegrees =
                double.NaN;
            solution.NextMaximumSurfaceDistanceMetres =
                double.NaN;
            solution.NextSetUT = double.NaN;

            solution.NextPlaneWindowUT = double.NaN;
            solution.PlaneBranch = "—";
            solution.LaunchAzimuthDegrees =
                double.NaN;
            solution.TargetElevationAtPlaneDegrees =
                double.NaN;
            solution.TargetRangeAtPlaneMetres =
                double.NaN;
            solution.TargetMotionAtPlane = "—";
            solution.NextWindowHeuristic = "—";
            solution.SearchSpanSeconds = double.NaN;
        }

        private static void Invalidate(string status)
        {
            solution.Valid = false;
            solution.Status = status;
            solution.TargetName = "None";

            solution.CurrentSlantRangeMetres =
                double.NaN;
            solution.CurrentRangeRateMetresPerSecond =
                double.NaN;
            solution.CurrentRelativeSpeedMetresPerSecond =
                double.NaN;
            solution.CurrentElevationDegrees =
                double.NaN;
            solution.CurrentAzimuthDegrees =
                double.NaN;
            solution.CurrentSurfaceDistanceMetres =
                double.NaN;
            solution.TargetMotion = "—";

            solution.InsideProximityReference = false;
            solution.ProximityMarginMetres =
                double.NaN;

            solution.ForecastReferenceMode = "—";
            solution.ReferenceLatitudeDegrees =
                double.NaN;
            solution.ReferenceLongitudeDegrees =
                double.NaN;

            ResetForecast();
            goodWindowSearch.Reset();
            goodWindowSearch.ClearSolution(
                solution);

            // Force immediate recomputation if the same target is selected
            // again after being cleared.
            lastForecastUT = double.NaN;
            lastTargetId = Guid.Empty;
        }
    }
}
