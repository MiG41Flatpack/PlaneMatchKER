// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

using KerbalEngineer.Flight.Readouts;
using PlaneMatchKER.Core;

namespace PlaneMatchKER.Readouts
{
    public sealed class PlaneMatchPanelReadout : ReadoutModule
    {
        public PlaneMatchPanelReadout()
        {
            Name = ModInfo.ReadoutName;
            ShortName = "Plane Match";
            Category =
                ReadoutCategory.GetCategory("Rendezvous");
            HelpString =
                "Shows signed target-plane offset, normal velocity, " +
                "relative inclination and an atmospheric manual bank cue.";
            IsDefault = false;
            Cloneable = false;
            CharacterLimit = 28;
            HudCharacterLimit = 28;
        }

        public override void Update()
        {
            PlaneMatchProcessor.Update();
        }

        public override void Draw(
            KerbalEngineer.Unity.Flight.ISectionModule section)
        {
            PlaneMatchProcessor.Update();

            PlaneMatchSolution solution =
                PlaneMatchProcessor.Solution;

            if (!solution.Valid)
            {
                DrawMessageLine(
                    solution.Status,
                    section.Width,
                    section.IsHud);
                return;
            }

            DrawLine(
                "Target",
                solution.TargetName,
                section);

            DrawLine(
                "Plane Offset",
                PlaneMatchFormatter.SignedDistance(
                    solution.PlaneOffsetMetres),
                section);

            DrawLine(
                "Plane Angle",
                PlaneMatchFormatter.Angle(
                    solution.PlaneAngleDegrees,
                    4),
                section);

            DrawLine(
                "Normal Velocity",
                PlaneMatchFormatter.Velocity(
                    solution.NormalVelocityMetresPerSecond),
                section);

            DrawLine(
                "Plane Error +20s",
                PlaneMatchFormatter.Angle(
                    solution.PredictedPlaneAngleDegrees,
                    4),
                section);

            DrawLine(
                "Linear Plane ETA",
                PlaneMatchFormatter.Time(
                    solution.LinearPlaneCrossingSeconds),
                section);

            DrawLine(
                "Relative Inclination",
                PlaneMatchFormatter.Angle(
                    solution.KerStyleRelativeInclinationDegrees,
                    4),
                section);

            DrawLine(
                "Atmospheric Bank Cue",
                solution.BankCue,
                section);

            DrawLine(
                "Capture",
                solution.CaptureStatus,
                section);

            if (solution.FlightDirectorAvailable)
            {
                DrawLine(
                    "Required Stop Bank",
                    PlaneMatchFormatter.Angle(
                        solution.RequiredStoppingBankDegrees,
                        2),
                    section);

                DrawLine(
                    "Projected Stop Offset",
                    PlaneMatchFormatter.SignedDistance(
                        solution.ProjectedStopOffsetMetres),
                    section);
            }

            DrawLine(
                "Horizontal Speed",
                PlaneMatchFormatter.Velocity(
                    solution.HorizontalInertialSpeed),
                section);
        }
    }
}
