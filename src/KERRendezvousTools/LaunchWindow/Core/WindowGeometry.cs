// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 LaunchWindowKER contributors

using System;

namespace LaunchWindowKER.Core
{
    internal static class WindowGeometry
    {
        internal const double DegreesPerRadian = 180.0 / Math.PI;
        internal const double RadiansPerDegree = Math.PI / 180.0;

        internal static Vector3d OrbitVectorToWorld(Vector3d value)
        {
            return new Vector3d(value.x, value.z, value.y);
        }

        internal static bool TryGetOrbitState(
            Orbit orbit,
            double universalTime,
            out Vector3d position,
            out Vector3d velocity)
        {
            position = Vector3d.zero;
            velocity = Vector3d.zero;

            if (orbit == null)
            {
                return false;
            }

            position = OrbitVectorToWorld(
                orbit.getRelativePositionAtUT(universalTime));

            velocity = OrbitVectorToWorld(
                orbit.getOrbitalVelocityAtUT(universalTime));

            return IsFinite(position) &&
                   IsFinite(velocity) &&
                   position.sqrMagnitude > 1.0e-12;
        }

        internal static bool TryGetVesselRelativePosition(
            Vessel vessel,
            CelestialBody body,
            out Vector3d relativePosition)
        {
            relativePosition = Vector3d.zero;

            if (vessel == null || body == null)
            {
                return false;
            }

            Vector3d vesselWorld =
                vessel.GetWorldPos3D();

            relativePosition =
                vesselWorld -
                body.position;

            return IsFinite(relativePosition) &&
                   relativePosition.sqrMagnitude > 1.0e-12;
        }

        internal static bool TryNormalize(
            Vector3d value,
            out Vector3d normalized)
        {
            normalized = Vector3d.zero;

            if (!IsFinite(value) ||
                value.sqrMagnitude < 1.0e-16)
            {
                return false;
            }

            normalized =
                value /
                value.magnitude;

            return IsFinite(normalized);
        }

        internal static bool TryOrbitNormal(
            Vector3d position,
            Vector3d velocity,
            out Vector3d normal)
        {
            return TryNormalize(
                Vector3d.Cross(position, velocity),
                out normal);
        }

        internal static Vector3d RotateAroundAxis(
            Vector3d vector,
            Vector3d unitAxis,
            double radians)
        {
            double cosine = Math.Cos(radians);
            double sine = Math.Sin(radians);

            return
                vector * cosine +
                Vector3d.Cross(unitAxis, vector) * sine +
                unitAxis *
                Vector3d.Dot(unitAxis, vector) *
                (1.0 - cosine);
        }

        internal static Vector3d SurfacePositionAtUT(
            Vector3d seaLevelPositionAtEpoch,
            CelestialBody body,
            double epochUT,
            double universalTime)
        {
            if (body == null ||
                !body.rotates ||
                !IsFinite(body.angularVelocity) ||
                body.angularVelocity.sqrMagnitude < 1.0e-18)
            {
                return seaLevelPositionAtEpoch;
            }

            Vector3d axis =
                body.angularVelocity /
                body.angularVelocity.magnitude;

            double angle =
                body.angularVelocity.magnitude *
                (universalTime - epochUT);

            return RotateAroundAxis(
                seaLevelPositionAtEpoch,
                axis,
                angle);
        }

        internal static bool TryRotationAxis(
            CelestialBody body,
            out Vector3d axis)
        {
            axis = Vector3d.zero;

            if (body == null)
            {
                return false;
            }

            if (body.rotates &&
                IsFinite(body.angularVelocity) &&
                body.angularVelocity.sqrMagnitude > 1.0e-18)
            {
                return TryNormalize(
                    body.angularVelocity,
                    out axis);
            }

            return TryNormalize(
                new Vector3d(
                    Planetarium.up.x,
                    Planetarium.up.y,
                    Planetarium.up.z),
                out axis);
        }

        internal static double ElevationDegrees(
            Vector3d sitePosition,
            Vector3d targetPosition)
        {
            Vector3d up;
            Vector3d lineOfSight;

            if (!TryNormalize(sitePosition, out up) ||
                !TryNormalize(
                    targetPosition - sitePosition,
                    out lineOfSight))
            {
                return double.NaN;
            }

            double sine =
                Clamp(
                    Vector3d.Dot(
                        lineOfSight,
                        up),
                    -1.0,
                    1.0);

            return
                Math.Asin(sine) *
                DegreesPerRadian;
        }

        internal static double SurfaceDistanceMetres(
            Vector3d sitePosition,
            Vector3d targetPosition,
            double bodyRadius)
        {
            Vector3d siteUnit;
            Vector3d targetUnit;

            if (!TryNormalize(sitePosition, out siteUnit) ||
                !TryNormalize(targetPosition, out targetUnit))
            {
                return double.NaN;
            }

            double cosine =
                Clamp(
                    Vector3d.Dot(
                        siteUnit,
                        targetUnit),
                    -1.0,
                    1.0);

            return
                bodyRadius *
                Math.Acos(cosine);
        }

        internal static double AzimuthDegrees(
            Vector3d sitePosition,
            Vector3d targetPosition,
            Vector3d rotationAxis)
        {
            Vector3d up;
            Vector3d lineOfSight;

            if (!TryNormalize(sitePosition, out up) ||
                !TryNormalize(
                    targetPosition - sitePosition,
                    out lineOfSight))
            {
                return double.NaN;
            }

            Vector3d north =
                rotationAxis -
                up *
                Vector3d.Dot(
                    rotationAxis,
                    up);

            if (!TryNormalize(north, out north))
            {
                return double.NaN;
            }

            Vector3d east =
                Vector3d.Cross(
                    north,
                    up);

            if (!TryNormalize(east, out east))
            {
                return double.NaN;
            }

            Vector3d horizontal =
                lineOfSight -
                up *
                Vector3d.Dot(
                    lineOfSight,
                    up);

            if (!TryNormalize(horizontal, out horizontal))
            {
                return double.NaN;
            }

            double azimuth =
                Math.Atan2(
                    Vector3d.Dot(
                        horizontal,
                        east),
                    Vector3d.Dot(
                        horizontal,
                        north)) *
                DegreesPerRadian;

            return Wrap360(azimuth);
        }

        internal static double LaunchAzimuthDegrees(
            Vector3d sitePosition,
            Vector3d targetOrbitNormal,
            Vector3d rotationAxis)
        {
            Vector3d up;

            if (!TryNormalize(sitePosition, out up))
            {
                return double.NaN;
            }

            Vector3d progradeTrack =
                Vector3d.Cross(
                    targetOrbitNormal,
                    up);

            if (!TryNormalize(
                    progradeTrack,
                    out progradeTrack))
            {
                return double.NaN;
            }

            Vector3d north =
                rotationAxis -
                up *
                Vector3d.Dot(
                    rotationAxis,
                    up);

            if (!TryNormalize(north, out north))
            {
                return double.NaN;
            }

            Vector3d east =
                Vector3d.Cross(
                    north,
                    up);

            if (!TryNormalize(east, out east))
            {
                return double.NaN;
            }

            return Wrap360(
                Math.Atan2(
                    Vector3d.Dot(
                        progradeTrack,
                        east),
                    Vector3d.Dot(
                        progradeTrack,
                        north)) *
                DegreesPerRadian);
        }

        internal static string PlaneBranch(
            Vector3d sitePosition,
            Vector3d targetOrbitNormal,
            Vector3d rotationAxis)
        {
            Vector3d up;

            if (!TryNormalize(sitePosition, out up))
            {
                return "—";
            }

            Vector3d progradeTrack =
                Vector3d.Cross(
                    targetOrbitNormal,
                    up);

            if (!TryNormalize(
                    progradeTrack,
                    out progradeTrack))
            {
                return "—";
            }

            Vector3d north =
                rotationAxis -
                up *
                Vector3d.Dot(
                    rotationAxis,
                    up);

            if (!TryNormalize(north, out north))
            {
                return "POLAR";
            }

            double northward =
                Vector3d.Dot(
                    progradeTrack,
                    north);

            if (Math.Abs(northward) < 1.0e-5)
            {
                return "EQUATORIAL";
            }

            return northward > 0.0
                ? "ASCENDING"
                : "DESCENDING";
        }

        internal static double Wrap360(double degrees)
        {
            if (!IsFinite(degrees))
            {
                return double.NaN;
            }

            degrees %= 360.0;

            if (degrees < 0.0)
            {
                degrees += 360.0;
            }

            return degrees;
        }

        internal static double Clamp(
            double value,
            double minimum,
            double maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        internal static bool IsFinite(double value)
        {
            return !double.IsNaN(value) &&
                   !double.IsInfinity(value);
        }

        internal static bool IsFinite(Vector3d value)
        {
            return IsFinite(value.x) &&
                   IsFinite(value.y) &&
                   IsFinite(value.z);
        }
    }
}
