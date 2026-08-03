// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

using System;

namespace PlaneMatchKER.Core
{
    internal static class PlaneGeometry
    {
        internal const double DegreesPerRadian = 180.0 / Math.PI;
        internal const double RadiansPerDegree = Math.PI / 180.0;

        internal static Vector3d OrbitVectorToWorld(Vector3d orbitVector)
        {
            return new Vector3d(
                orbitVector.x,
                orbitVector.z,
                orbitVector.y);
        }

        internal static bool TryGetWorldState(
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

            normalized = value / value.magnitude;
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

        internal static Vector3d Reject(
            Vector3d vector,
            Vector3d unitNormal)
        {
            return vector -
                   unitNormal *
                   Vector3d.Dot(vector, unitNormal);
        }

        internal static double AngleDegrees(
            Vector3d firstUnit,
            Vector3d secondUnit)
        {
            double dot = Clamp(
                Vector3d.Dot(firstUnit, secondUnit),
                -1.0,
                1.0);

            return Math.Acos(dot) *
                   DegreesPerRadian;
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

        internal static Vector3d ToVector3d(
            UnityEngine.Vector3 value)
        {
            return new Vector3d(
                value.x,
                value.y,
                value.z);
        }
    }
}
