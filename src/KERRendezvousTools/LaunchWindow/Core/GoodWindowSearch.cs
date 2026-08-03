// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 KER Rendezvous Tools contributors

using System;

namespace LaunchWindowKER.Core
{
    internal sealed class GoodWindowSearch
    {
        internal const int MaximumPlaneWindows = 8192;

        private const int CandidatesPerUpdate = 32;
        private const double GoodElevationDegrees = 5.0;
        private const double EventExpiredToleranceSeconds = 0.5;
        // Search results must not be invalidated by ordinary floating-point
        // jitter in KSP's live orbit and surface transforms. These thresholds
        // represent material changes, not per-frame numerical noise.
        private const double PeriodRelativeTolerance = 1.0e-4;
        private const double OrbitNormalDotTolerance = 3.8077175e-7;
        private const double ReferenceSiteDotTolerance = 1.5230871e-8;
        private const double RecurrenceDirectionDotTolerance = 1.0e-10;
        private const double RecurrenceRadiusRelativeTolerance = 1.0e-8;
        private const double DuplicateRootToleranceSeconds = 0.05;
        private const double TwoPi = 2.0 * Math.PI;

        private bool configured;
        private bool searching;
        private bool found;
        private bool recurrenceDetected;
        private bool limitReached;
        private bool continuousPlane;

        private Guid vesselId = Guid.Empty;
        private Guid targetId = Guid.Empty;
        private CelestialBody body;

        private double targetPeriod = double.NaN;
        private double rotationPeriod = double.NaN;
        private double referenceLatitudeDegrees = double.NaN;
        private double referenceLongitudeDegrees = double.NaN;

        private Vector3d referenceSiteAtEpoch = Vector3d.zero;
        private double referenceEpoch = double.NaN;
        private Vector3d targetOrbitNormal = Vector3d.zero;
        private Vector3d rotationAxis = Vector3d.zero;

        private bool hasSequenceA;
        private bool hasSequenceB;
        private double nextSequenceAUT = double.NaN;
        private double nextSequenceBUT = double.NaN;

        private int evaluatedWindows;
        private double lastEvaluatedUT = double.NaN;

        private double nextGoodUT = double.NaN;
        private int goodWindowNumber;
        private string goodBranch = "—";
        private double goodLaunchAzimuthDegrees = double.NaN;
        private double goodElevationDegrees = double.NaN;

        private double bestRisingUT = double.NaN;
        private double bestRisingElevationDegrees = double.NaN;
        private string bestRisingBranch = "—";
        private double bestRisingLaunchAzimuthDegrees = double.NaN;

        private bool recurrenceReferenceStored;
        private Vector3d recurrenceSiteUnit = Vector3d.zero;
        private Vector3d recurrenceTargetUnit = Vector3d.zero;
        private double recurrenceTargetRadius = double.NaN;
        private double recurrenceReferenceUT = double.NaN;

        private string status = "NOT STARTED";

        internal void Reset()
        {
            configured = false;
            searching = false;
            found = false;
            recurrenceDetected = false;
            limitReached = false;
            continuousPlane = false;

            vesselId = Guid.Empty;
            targetId = Guid.Empty;
            body = null;

            targetPeriod = double.NaN;
            rotationPeriod = double.NaN;
            referenceLatitudeDegrees = double.NaN;
            referenceLongitudeDegrees = double.NaN;

            referenceSiteAtEpoch = Vector3d.zero;
            referenceEpoch = double.NaN;
            targetOrbitNormal = Vector3d.zero;
            rotationAxis = Vector3d.zero;

            hasSequenceA = false;
            hasSequenceB = false;
            nextSequenceAUT = double.NaN;
            nextSequenceBUT = double.NaN;

            evaluatedWindows = 0;
            lastEvaluatedUT = double.NaN;

            nextGoodUT = double.NaN;
            goodWindowNumber = 0;
            goodBranch = "—";
            goodLaunchAzimuthDegrees = double.NaN;
            goodElevationDegrees = double.NaN;

            bestRisingUT = double.NaN;
            bestRisingElevationDegrees = double.NaN;
            bestRisingBranch = "—";
            bestRisingLaunchAzimuthDegrees = double.NaN;

            recurrenceReferenceStored = false;
            recurrenceSiteUnit = Vector3d.zero;
            recurrenceTargetUnit = Vector3d.zero;
            recurrenceTargetRadius = double.NaN;
            recurrenceReferenceUT = double.NaN;

            status = "NOT STARTED";
        }

        internal void EnsureConfigured(
            Guid currentVesselId,
            Guid currentTargetId,
            Orbit targetOrbit,
            CelestialBody currentBody,
            Vector3d currentReferenceSiteAtEpoch,
            double currentReferenceEpoch,
            Vector3d currentTargetOrbitNormal,
            Vector3d currentRotationAxis,
            double nowUT,
            double currentReferenceLatitudeDegrees,
            double currentReferenceLongitudeDegrees)
        {
            bool orbitPeriodChanged =
                RelativeDifference(
                    targetOrbit.period,
                    targetPeriod) >
                    PeriodRelativeTolerance;

            bool orbitPlaneChanged =
                DirectionChanged(
                    currentTargetOrbitNormal,
                    targetOrbitNormal,
                    OrbitNormalDotTolerance);

            bool referenceSiteChanged =
                ReferenceSiteChanged(
                    currentBody,
                    currentReferenceSiteAtEpoch,
                    currentReferenceEpoch,
                    nowUT);

            bool signatureChanged =
                !configured ||
                currentVesselId != vesselId ||
                currentTargetId != targetId ||
                currentBody != body ||
                orbitPeriodChanged ||
                orbitPlaneChanged ||
                referenceSiteChanged;

            bool resultExpired =
                found &&
                nextGoodUT <
                    nowUT -
                    EventExpiredToleranceSeconds;

            bool searchFellBehind =
                searching &&
                WindowGeometry.IsFinite(
                    PeekNextCandidateUT()) &&
                PeekNextCandidateUT() <
                    nowUT -
                    EventExpiredToleranceSeconds;

            bool exhaustedSearchExpired =
                !searching &&
                !found &&
                WindowGeometry.IsFinite(
                    lastEvaluatedUT) &&
                lastEvaluatedUT <
                    nowUT -
                    EventExpiredToleranceSeconds;

            if (signatureChanged ||
                resultExpired ||
                searchFellBehind ||
                exhaustedSearchExpired)
            {
                Start(
                    currentVesselId,
                    currentTargetId,
                    targetOrbit,
                    currentBody,
                    currentReferenceSiteAtEpoch,
                    currentReferenceEpoch,
                    currentTargetOrbitNormal,
                    currentRotationAxis,
                    nowUT,
                    currentReferenceLatitudeDegrees,
                    currentReferenceLongitudeDegrees);
            }
        }

        internal void Step(
            Orbit targetOrbit,
            double nowUT)
        {
            if (!configured ||
                !searching)
            {
                return;
            }

            for (int index = 0;
                 index < CandidatesPerUpdate &&
                 searching;
                 index++)
            {
                double candidateUT =
                    TakeNextCandidateUT();

                if (!WindowGeometry.IsFinite(
                        candidateUT))
                {
                    searching = false;
                    status = "NO FUTURE PLANE WINDOW";
                    break;
                }

                if (candidateUT <
                    nowUT -
                    EventExpiredToleranceSeconds)
                {
                    continue;
                }

                evaluatedWindows++;
                lastEvaluatedUT = candidateUT;

                EvaluateCandidate(
                    targetOrbit,
                    candidateUT);

                if (found)
                {
                    searching = false;
                    status = "FOUND";
                    break;
                }

                if (recurrenceDetected)
                {
                    searching = false;
                    status = "NONE IN RECURRENCE";
                    break;
                }

                if (evaluatedWindows >=
                    MaximumPlaneWindows)
                {
                    searching = false;
                    limitReached = true;
                    status = "SEARCH LIMIT REACHED";
                    break;
                }
            }
        }

        internal void PopulateSolution(
            LaunchWindowSolution solution,
            double nowUT)
        {
            solution.GoodSearchStatus = status;
            solution.GoodSearchEvaluatedWindows =
                evaluatedWindows;
            solution.GoodSearchMaximumWindows =
                MaximumPlaneWindows;
            solution.GoodSearchFound = found;
            solution.GoodSearchRecurrenceDetected =
                recurrenceDetected;
            solution.GoodSearchLimitReached =
                limitReached;

            solution.NextGoodUT =
                found
                    ? nextGoodUT
                    : double.NaN;

            solution.GoodWindowNumber =
                found
                    ? goodWindowNumber
                    : 0;

            solution.GoodBranch =
                found
                    ? goodBranch
                    : "—";

            solution.GoodLaunchAzimuthDegrees =
                found
                    ? goodLaunchAzimuthDegrees
                    : double.NaN;

            solution.GoodElevationDegrees =
                found
                    ? goodElevationDegrees
                    : double.NaN;

            if (found)
            {
                double remainingSeconds =
                    Math.Max(
                        0.0,
                        nextGoodUT -
                        nowUT);

                solution.TargetOrbitsToGood =
                    remainingSeconds /
                    targetPeriod;

                solution.BodyRotationsToGood =
                    WindowGeometry.IsFinite(
                        rotationPeriod) &&
                    rotationPeriod > 0.0
                        ? remainingSeconds /
                            rotationPeriod
                        : double.NaN;
            }
            else
            {
                solution.TargetOrbitsToGood =
                    double.NaN;
                solution.BodyRotationsToGood =
                    double.NaN;
            }

            solution.BestRisingUT =
                bestRisingUT;
            solution.BestRisingElevationDegrees =
                bestRisingElevationDegrees;
            solution.BestRisingBranch =
                bestRisingBranch;
            solution.BestRisingLaunchAzimuthDegrees =
                bestRisingLaunchAzimuthDegrees;

            if (continuousPlane &&
                found)
            {
                solution.GoodSearchStatus =
                    "CONTINUOUS PLANE / NEXT RISE";
            }
        }

        internal void ClearSolution(
            LaunchWindowSolution solution)
        {
            solution.GoodSearchStatus = "—";
            solution.GoodSearchEvaluatedWindows = 0;
            solution.GoodSearchMaximumWindows =
                MaximumPlaneWindows;
            solution.GoodSearchFound = false;
            solution.GoodSearchRecurrenceDetected = false;
            solution.GoodSearchLimitReached = false;

            solution.NextGoodUT = double.NaN;
            solution.TargetOrbitsToGood = double.NaN;
            solution.BodyRotationsToGood = double.NaN;
            solution.GoodWindowNumber = 0;
            solution.GoodBranch = "—";
            solution.GoodLaunchAzimuthDegrees =
                double.NaN;
            solution.GoodElevationDegrees =
                double.NaN;

            solution.BestRisingUT = double.NaN;
            solution.BestRisingElevationDegrees =
                double.NaN;
            solution.BestRisingBranch = "—";
            solution.BestRisingLaunchAzimuthDegrees =
                double.NaN;
        }

        private void Start(
            Guid currentVesselId,
            Guid currentTargetId,
            Orbit targetOrbit,
            CelestialBody currentBody,
            Vector3d currentReferenceSiteAtEpoch,
            double currentReferenceEpoch,
            Vector3d currentTargetOrbitNormal,
            Vector3d currentRotationAxis,
            double nowUT,
            double currentReferenceLatitudeDegrees,
            double currentReferenceLongitudeDegrees)
        {
            Reset();

            configured = true;
            vesselId = currentVesselId;
            targetId = currentTargetId;
            body = currentBody;

            targetPeriod = targetOrbit.period;
            rotationPeriod =
                currentBody.rotates &&
                WindowGeometry.IsFinite(
                    currentBody.rotationPeriod) &&
                Math.Abs(
                    currentBody.rotationPeriod) > 1.0
                    ? Math.Abs(
                        currentBody.rotationPeriod)
                    : double.NaN;

            referenceLatitudeDegrees =
                currentReferenceLatitudeDegrees;
            referenceLongitudeDegrees =
                currentReferenceLongitudeDegrees;

            referenceSiteAtEpoch =
                currentReferenceSiteAtEpoch;
            referenceEpoch =
                currentReferenceEpoch;
            targetOrbitNormal =
                currentTargetOrbitNormal;
            rotationAxis =
                currentRotationAxis;

            if (TryInitializePlaneSequences(
                    nowUT))
            {
                searching = true;
                status = "SEARCHING";
                return;
            }

            if (continuousPlane)
            {
                if (TryFindNextRise(
                        targetOrbit,
                        nowUT,
                        out nextGoodUT))
                {
                    found = true;
                    goodWindowNumber = 1;
                    goodBranch = "CONTINUOUS";
                    goodElevationDegrees = 0.0;

                    Vector3d site =
                        WindowGeometry.SurfacePositionAtUT(
                            referenceSiteAtEpoch,
                            body,
                            referenceEpoch,
                            nextGoodUT);

                    goodLaunchAzimuthDegrees =
                        WindowGeometry.LaunchAzimuthDegrees(
                            site,
                            targetOrbitNormal,
                            rotationAxis);

                    status =
                        "CONTINUOUS PLANE / NEXT RISE";
                }
                else
                {
                    status =
                        "CONTINUOUS PLANE / NO RISE";
                }

                return;
            }

            status = "NO ACCESSIBLE PLANE";
        }

        private bool TryInitializePlaneSequences(
            double nowUT)
        {
            if (body == null ||
                !body.rotates ||
                !WindowGeometry.IsFinite(
                    body.angularVelocity) ||
                body.angularVelocity.sqrMagnitude <
                    1.0e-18)
            {
                return false;
            }

            double omega =
                body.angularVelocity.magnitude;

            Vector3d axis =
                rotationAxis;

            double siteAxisComponent =
                Vector3d.Dot(
                    axis,
                    referenceSiteAtEpoch);

            Vector3d parallel =
                axis *
                siteAxisComponent;

            Vector3d perpendicular =
                referenceSiteAtEpoch -
                parallel;

            double a =
                Vector3d.Dot(
                    targetOrbitNormal,
                    parallel);

            double b =
                Vector3d.Dot(
                    targetOrbitNormal,
                    perpendicular);

            double c =
                Vector3d.Dot(
                    targetOrbitNormal,
                    Vector3d.Cross(
                        axis,
                        referenceSiteAtEpoch));

            double amplitude =
                Math.Sqrt(
                    b * b +
                    c * c);

            double scale =
                Math.Max(
                    1.0,
                    referenceSiteAtEpoch.magnitude);

            if (amplitude <
                scale * 1.0e-12)
            {
                continuousPlane =
                    Math.Abs(a) <
                    scale * 1.0e-10;

                return false;
            }

            double ratio =
                -a /
                amplitude;

            if (ratio < -1.0 - 1.0e-12 ||
                ratio > 1.0 + 1.0e-12)
            {
                return false;
            }

            ratio =
                WindowGeometry.Clamp(
                    ratio,
                    -1.0,
                    1.0);

            double phase =
                Math.Atan2(
                    c,
                    b);

            double offset =
                Math.Acos(
                    ratio);

            double rootAngleA =
                NormalizeAngle(
                    phase -
                    offset);

            double rootAngleB =
                NormalizeAngle(
                    phase +
                    offset);

            nextSequenceAUT =
                FirstRootAtOrAfter(
                    rootAngleA,
                    omega,
                    nowUT);

            hasSequenceA =
                WindowGeometry.IsFinite(
                    nextSequenceAUT);

            double candidateB =
                FirstRootAtOrAfter(
                    rootAngleB,
                    omega,
                    nowUT);

            if (Math.Abs(
                    candidateB -
                    nextSequenceAUT) <=
                DuplicateRootToleranceSeconds)
            {
                hasSequenceB = false;
                nextSequenceBUT = double.NaN;
            }
            else
            {
                hasSequenceB = true;
                nextSequenceBUT = candidateB;
            }

            rotationPeriod =
                TwoPi /
                omega;

            return
                hasSequenceA ||
                hasSequenceB;
        }

        private double FirstRootAtOrAfter(
            double rootAngle,
            double omega,
            double nowUT)
        {
            double angleAtNow =
                omega *
                (nowUT -
                 referenceEpoch);

            double cycles =
                Math.Ceiling(
                    (angleAtNow -
                     rootAngle) /
                    TwoPi);

            double root =
                rootAngle +
                cycles *
                TwoPi;

            if (root <
                angleAtNow -
                1.0e-12)
            {
                root += TwoPi;
            }

            return
                referenceEpoch +
                root /
                omega;
        }

        private double PeekNextCandidateUT()
        {
            if (hasSequenceA &&
                hasSequenceB)
            {
                return Math.Min(
                    nextSequenceAUT,
                    nextSequenceBUT);
            }

            if (hasSequenceA)
            {
                return nextSequenceAUT;
            }

            if (hasSequenceB)
            {
                return nextSequenceBUT;
            }

            return double.NaN;
        }

        private double TakeNextCandidateUT()
        {
            if (!hasSequenceA &&
                !hasSequenceB)
            {
                return double.NaN;
            }

            if (hasSequenceA &&
                hasSequenceB &&
                Math.Abs(
                    nextSequenceAUT -
                    nextSequenceBUT) <=
                    DuplicateRootToleranceSeconds)
            {
                double candidate =
                    0.5 *
                    (nextSequenceAUT +
                     nextSequenceBUT);

                nextSequenceAUT +=
                    rotationPeriod;
                nextSequenceBUT +=
                    rotationPeriod;

                return candidate;
            }

            if (hasSequenceA &&
                (!hasSequenceB ||
                 nextSequenceAUT <
                    nextSequenceBUT))
            {
                double candidate =
                    nextSequenceAUT;

                nextSequenceAUT +=
                    rotationPeriod;

                return candidate;
            }

            double result =
                nextSequenceBUT;

            nextSequenceBUT +=
                rotationPeriod;

            return result;
        }

        private void EvaluateCandidate(
            Orbit targetOrbit,
            double candidateUT)
        {
            Vector3d site =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    candidateUT);

            Vector3d target =
                TargetPositionAt(
                    targetOrbit,
                    candidateUT);

            double elevation =
                WindowGeometry.ElevationDegrees(
                    site,
                    target);

            string motion =
                MotionAt(
                    targetOrbit,
                    candidateUT);

            string branch =
                WindowGeometry.PlaneBranch(
                    site,
                    targetOrbitNormal,
                    rotationAxis);

            double azimuth =
                WindowGeometry.LaunchAzimuthDegrees(
                    site,
                    targetOrbitNormal,
                    rotationAxis);

            bool rising =
                motion == "RISING";

            if (rising &&
                (!WindowGeometry.IsFinite(
                     bestRisingElevationDegrees) ||
                 Math.Abs(elevation) <
                    Math.Abs(
                        bestRisingElevationDegrees)))
            {
                bestRisingUT = candidateUT;
                bestRisingElevationDegrees =
                    elevation;
                bestRisingBranch =
                    branch;
                bestRisingLaunchAzimuthDegrees =
                    azimuth;
            }

            if (rising &&
                WindowGeometry.IsFinite(
                    elevation) &&
                Math.Abs(elevation) <=
                    GoodElevationDegrees)
            {
                found = true;
                nextGoodUT = candidateUT;
                goodWindowNumber =
                    evaluatedWindows;
                goodBranch =
                    branch;
                goodLaunchAzimuthDegrees =
                    azimuth;
                goodElevationDegrees =
                    elevation;
                return;
            }

            CheckRecurrence(
                site,
                target,
                candidateUT);
        }

        private void CheckRecurrence(
            Vector3d site,
            Vector3d target,
            double candidateUT)
        {
            Vector3d siteUnit;
            Vector3d targetUnit;

            if (!WindowGeometry.TryNormalize(
                    site,
                    out siteUnit) ||
                !WindowGeometry.TryNormalize(
                    target,
                    out targetUnit))
            {
                return;
            }

            double targetRadius =
                target.magnitude;

            if (!recurrenceReferenceStored)
            {
                recurrenceReferenceStored = true;
                recurrenceSiteUnit =
                    siteUnit;
                recurrenceTargetUnit =
                    targetUnit;
                recurrenceTargetRadius =
                    targetRadius;
                recurrenceReferenceUT =
                    candidateUT;
                return;
            }

            if (candidateUT -
                recurrenceReferenceUT <
                Math.Max(
                    targetPeriod,
                    WindowGeometry.IsFinite(
                        rotationPeriod)
                        ? rotationPeriod
                        : 0.0))
            {
                return;
            }

            double siteDot =
                Vector3d.Dot(
                    siteUnit,
                    recurrenceSiteUnit);

            double targetDot =
                Vector3d.Dot(
                    targetUnit,
                    recurrenceTargetUnit);

            double radiusDifference =
                RelativeDifference(
                    targetRadius,
                    recurrenceTargetRadius);

            if (siteDot >=
                    1.0 -
                    RecurrenceDirectionDotTolerance &&
                targetDot >=
                    1.0 -
                    RecurrenceDirectionDotTolerance &&
                radiusDifference <=
                    RecurrenceRadiusRelativeTolerance)
            {
                recurrenceDetected = true;
            }
        }

        private bool TryFindNextRise(
            Orbit targetOrbit,
            double nowUT,
            out double riseUT)
        {
            riseUT = double.NaN;

            double span =
                Math.Max(
                    targetPeriod * 8.0,
                    WindowGeometry.IsFinite(
                        rotationPeriod)
                        ? rotationPeriod
                        : targetPeriod);

            int samples = 720;
            double step =
                span /
                samples;

            double previousUT =
                nowUT;

            double previousElevation =
                ElevationAt(
                    targetOrbit,
                    nowUT);

            for (int index = 1;
                 index <= samples;
                 index++)
            {
                double currentUT =
                    nowUT +
                    step *
                    index;

                double currentElevation =
                    ElevationAt(
                        targetOrbit,
                        currentUT);

                if (previousElevation < 0.0 &&
                    currentElevation >= 0.0)
                {
                    riseUT =
                        RefineRise(
                            targetOrbit,
                            previousUT,
                            currentUT);

                    return true;
                }

                previousUT =
                    currentUT;
                previousElevation =
                    currentElevation;
            }

            return false;
        }

        private double RefineRise(
            Orbit targetOrbit,
            double lowerUT,
            double upperUT)
        {
            double lower =
                ElevationAt(
                    targetOrbit,
                    lowerUT);

            for (int iteration = 0;
                 iteration < 48 &&
                 upperUT -
                    lowerUT >
                    0.05;
                 iteration++)
            {
                double middleUT =
                    0.5 *
                    (lowerUT +
                     upperUT);

                double middle =
                    ElevationAt(
                        targetOrbit,
                        middleUT);

                if (lower *
                    middle <= 0.0)
                {
                    upperUT =
                        middleUT;
                }
                else
                {
                    lowerUT =
                        middleUT;
                    lower =
                        middle;
                }
            }

            return
                0.5 *
                (lowerUT +
                 upperUT);
        }

        private double ElevationAt(
            Orbit targetOrbit,
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

        private string MotionAt(
            Orbit targetOrbit,
            double universalTime)
        {
            double before =
                ElevationAt(
                    targetOrbit,
                    universalTime -
                    0.5);

            double after =
                ElevationAt(
                    targetOrbit,
                    universalTime +
                    0.5);

            if (!WindowGeometry.IsFinite(before) ||
                !WindowGeometry.IsFinite(after))
            {
                return "—";
            }

            double rate =
                after -
                before;

            if (Math.Abs(rate) <
                1.0e-4)
            {
                return "CULMINATING";
            }

            return
                rate > 0.0
                    ? "RISING"
                    : "SETTING";
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

        private bool ReferenceSiteChanged(
            CelestialBody currentBody,
            Vector3d currentReferenceSiteAtEpoch,
            double currentReferenceEpoch,
            double nowUT)
        {
            if (!configured ||
                currentBody == null ||
                body == null ||
                currentBody != body)
            {
                return true;
            }

            Vector3d storedSiteNow =
                WindowGeometry.SurfacePositionAtUT(
                    referenceSiteAtEpoch,
                    body,
                    referenceEpoch,
                    nowUT);

            Vector3d currentSiteNow =
                WindowGeometry.SurfacePositionAtUT(
                    currentReferenceSiteAtEpoch,
                    currentBody,
                    currentReferenceEpoch,
                    nowUT);

            return
                DirectionChanged(
                    currentSiteNow,
                    storedSiteNow,
                    ReferenceSiteDotTolerance);
        }

        private static bool DirectionChanged(
            Vector3d currentDirection,
            Vector3d storedDirection,
            double dotTolerance)
        {
            Vector3d currentUnit;
            Vector3d storedUnit;

            if (!WindowGeometry.TryNormalize(
                    currentDirection,
                    out currentUnit) ||
                !WindowGeometry.TryNormalize(
                    storedDirection,
                    out storedUnit))
            {
                return true;
            }

            double dot =
                WindowGeometry.Clamp(
                    Vector3d.Dot(
                        currentUnit,
                        storedUnit),
                    -1.0,
                    1.0);

            return
                dot <
                1.0 -
                dotTolerance;
        }

        private static double NormalizeAngle(
            double radians)
        {
            radians %= TwoPi;

            if (radians < 0.0)
            {
                radians += TwoPi;
            }

            return radians;
        }

        private static double RelativeDifference(
            double first,
            double second)
        {
            if (!WindowGeometry.IsFinite(first) ||
                !WindowGeometry.IsFinite(second))
            {
                return double.PositiveInfinity;
            }

            double scale =
                Math.Max(
                    1.0,
                    Math.Max(
                        Math.Abs(first),
                        Math.Abs(second)));

            return
                Math.Abs(
                    first -
                    second) /
                scale;
        }

    }
}
