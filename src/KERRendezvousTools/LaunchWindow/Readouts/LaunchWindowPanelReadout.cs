// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 LaunchWindowKER contributors

using KerbalEngineer.Flight.Readouts;
using LaunchWindowKER.Core;

namespace LaunchWindowKER.Readouts
{
    public sealed class LaunchWindowPanelReadout : ReadoutModule
    {
        public LaunchWindowPanelReadout()
        {
            Name = ModInfo.ReadoutName;
            ShortName = "Launch Window";
            Category =
                ReadoutCategory.GetCategory("Rendezvous");
            HelpString =
                "Shows live target geometry, sea-level pass timing, " +
                "and the next launch-site target-plane crossing.";
            IsDefault = false;
            Cloneable = false;
            CharacterLimit = 31;
            HudCharacterLimit = 31;
        }

        public override void Update()
        {
            LaunchWindowProcessor.Update();
        }

        public override void Draw(
            KerbalEngineer.Unity.Flight.ISectionModule section)
        {
            LaunchWindowProcessor.Update();

            LaunchWindowSolution solution =
                LaunchWindowProcessor.Solution;

            if (!solution.Valid)
            {
                DrawMessageLine(
                    solution.Status,
                    section.Width,
                    section.IsHud);
                return;
            }

            double nowUT =
                Planetarium.GetUniversalTime();

            DrawLine(
                "Target",
                solution.TargetName,
                section);

            DrawLine(
                "Slant Range",
                LaunchWindowFormatter.Distance(
                    solution.CurrentSlantRangeMetres),
                section);

            DrawLine(
                "Range Rate (+ opening)",
                LaunchWindowFormatter.SignedVelocity(
                    solution.CurrentRangeRateMetresPerSecond),
                section);

            DrawLine(
                "Relative Speed",
                LaunchWindowFormatter.Velocity(
                    solution.CurrentRelativeSpeedMetresPerSecond),
                section);

            DrawLine(
                "Sea-Level Elevation",
                LaunchWindowFormatter.Angle(
                    solution.CurrentElevationDegrees,
                    2),
                section);

            DrawLine(
                "Target Azimuth",
                LaunchWindowFormatter.Azimuth(
                    solution.CurrentAzimuthDegrees),
                section);

            DrawLine(
                "Surface-Track Distance",
                LaunchWindowFormatter.Distance(
                    solution.CurrentSurfaceDistanceMetres),
                section);

            DrawLine(
                "Target Motion",
                solution.TargetMotion,
                section);

            DrawLine(
                "2.5 km Proximity",
                solution.InsideProximityReference
                    ? "INSIDE"
                    : "OUTSIDE",
                section);

            DrawLine(
                "2.5 km Margin",
                LaunchWindowFormatter.SignedDistance(
                    solution.ProximityMarginMetres),
                section);

            DrawLine(
                "Forecast Reference",
                solution.ForecastReferenceMode,
                section);

            DrawLine(
                "Next Rise @ Ref",
                LaunchWindowFormatter.Countdown(
                    solution.NextRiseUT,
                    nowUT),
                section);

            DrawLine(
                "Next Max @ Ref",
                LaunchWindowFormatter.Countdown(
                    solution.NextMaximumUT,
                    nowUT),
                section);

            DrawLine(
                "Max Elevation @ Ref",
                LaunchWindowFormatter.Angle(
                    solution.NextMaximumElevationDegrees,
                    2),
                section);

            DrawLine(
                "Pass Ground Distance",
                LaunchWindowFormatter.Distance(
                    solution.NextMaximumSurfaceDistanceMetres),
                section);

            DrawLine(
                "Next Set @ Ref",
                LaunchWindowFormatter.Countdown(
                    solution.NextSetUT,
                    nowUT),
                section);

            DrawLine(
                "Next Plane @ Ref",
                LaunchWindowFormatter.Countdown(
                    solution.NextPlaneWindowUT,
                    nowUT),
                section);

            DrawLine(
                "Plane Branch",
                solution.PlaneBranch,
                section);

            DrawLine(
                "Plane Launch Azimuth",
                LaunchWindowFormatter.Azimuth(
                    solution.LaunchAzimuthDegrees),
                section);

            DrawLine(
                "Target Elev @ Plane",
                LaunchWindowFormatter.Angle(
                    solution.TargetElevationAtPlaneDegrees,
                    2),
                section);

            DrawLine(
                "Target Range @ Plane",
                LaunchWindowFormatter.Distance(
                    solution.TargetRangeAtPlaneMetres),
                section);

            DrawLine(
                "Motion @ Plane",
                solution.TargetMotionAtPlane,
                section);

            DrawLine(
                "Next-Window Heuristic",
                solution.NextWindowHeuristic,
                section);

            DrawLine(
                "Reference Latitude",
                LaunchWindowFormatter.Latitude(
                    solution.ReferenceLatitudeDegrees),
                section);

            DrawLine(
                "Reference Longitude",
                LaunchWindowFormatter.Longitude(
                    solution.ReferenceLongitudeDegrees),
                section);
        }
    }
}
