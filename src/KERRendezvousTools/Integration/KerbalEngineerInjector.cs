// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 KER Rendezvous Tools contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KerbalEngineer.Flight;
using KerbalEngineer.Flight.Readouts;
using KerbalEngineer.Flight.Sections;
using KERRendezvousTools.Core;
using LaunchWindowKER.Core;
using LaunchWindowKER.Readouts;
using PlaneMatchKER.Core;
using PlaneMatchKER.Readouts;
using UnityEngine;
using PlaneInfo = PlaneMatchKER.Core.ModInfo;
using WindowInfo = LaunchWindowKER.Core.ModInfo;

namespace KERRendezvousTools.Integration
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class KerbalEngineerInjector : MonoBehaviour
    {
        private const int IntegrityCheckIntervalFrames = 120;

        private PlaneMatchPanelReadout planeReadout;
        private LaunchWindowPanelReadout windowReadout;
        private SectionModule planeSection;
        private SectionModule windowSection;

        private bool installed;
        private bool lastPlaneButtonState;
        private bool lastWindowButtonState;
        private int nextIntegrityCheckFrame;

        private void Start()
        {
            StartCoroutine(InstallWhenReady());
        }

        private IEnumerator InstallWhenReady()
        {
            string legacyAssemblies = FindLegacyAssemblies();

            if (!string.IsNullOrEmpty(legacyAssemblies))
            {
                Debug.LogError(
                    SuiteInfo.LogPrefix +
                    " legacy standalone assemblies are loaded: " +
                    legacyAssemblies +
                    ". Remove GameData/PlaneMatchKER and " +
                    "GameData/LaunchWindowKER, then restart KSP.");

                yield break;
            }

            while (FlightEngineerCore.Instance == null ||
                   DisplayStack.Instance == null)
            {
                yield return null;
            }

            // KER loads saved section state during Flight startup. Delay two
            // frames so this suite can replace stale serialized PLNE/WIND
            // sections left by earlier standalone installations.
            yield return null;
            yield return null;

            InstallOrRepair();
        }

        private static string FindLegacyAssemblies()
        {
            string[] names =
                AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Select(
                        assembly =>
                            assembly.GetName().Name)
                    .Where(
                        name =>
                            string.Equals(
                                name,
                                "PlaneMatchKER",
                                StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(
                                name,
                                "LaunchWindowKER",
                                StringComparison.OrdinalIgnoreCase))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            return string.Join(", ", names);
        }

        private void InstallOrRepair()
        {
            try
            {
                RemoveRuntimeObjects();
                RegisterReadouts();
                RemoveStaleSections();

                planeSection =
                    CreateSection(
                        PlaneInfo.SectionName,
                        PlaneInfo.SectionAbbreviation,
                        planeReadout);

                windowSection =
                    CreateSection(
                        WindowInfo.SectionName,
                        WindowInfo.SectionAbbreviation,
                        windowReadout);

                SectionLibrary.CustomSections.Add(
                    windowSection);

                SectionLibrary.CustomSections.Add(
                    planeSection);

                windowReadout.Reset();
                planeReadout.Reset();

                installed = true;
                lastPlaneButtonState = false;
                lastWindowButtonState = false;
                nextIntegrityCheckFrame =
                    Time.frameCount +
                    IntegrityCheckIntervalFrames;

                RefreshVisibility(true);
                RequestDisplayResize();

                Debug.Log(
                    SuiteInfo.LogPrefix +
                    " " +
                    SuiteInfo.Version +
                    " registered WIND and PLNE sections in KER.");
            }
            catch (Exception exception)
            {
                installed = false;

                Debug.LogError(
                    SuiteInfo.LogPrefix +
                    " installation failed: " +
                    exception);
            }
        }

        private static SectionModule CreateSection(
            string name,
            string abbreviation,
            ReadoutModule readout)
        {
            return new SectionModule
            {
                Name = name,
                Abbreviation = abbreviation,
                IsVisible = false,
                IsFloating = false,
                showEditButton = false,
                showFloatButton = true,
                showButton = false,
                ReadoutModules =
                    new List<ReadoutModule>
                    {
                        readout
                    }
            };
        }

        private void RegisterReadouts()
        {
            ReadoutLibrary.Readouts.RemoveAll(
                item =>
                    IsPlaneReadout(item) ||
                    IsWindowReadout(item));

            planeReadout =
                new PlaneMatchPanelReadout();

            windowReadout =
                new LaunchWindowPanelReadout();

            ReadoutLibrary.Readouts.Add(
                windowReadout);

            ReadoutLibrary.Readouts.Add(
                planeReadout);
        }

        private static bool IsPlaneReadout(
            ReadoutModule item)
        {
            return
                item != null &&
                (item.GetType() ==
                     typeof(PlaneMatchPanelReadout) ||
                 item.Name ==
                     PlaneInfo.ReadoutName ||
                 item.Name ==
                     PlaneInfo.LegacyReadoutName ||
                 item.Name ==
                     "Plane Match Panel");
        }

        private static bool IsWindowReadout(
            ReadoutModule item)
        {
            return
                item != null &&
                (item.GetType() ==
                     typeof(LaunchWindowPanelReadout) ||
                 item.Name ==
                     WindowInfo.ReadoutName ||
                 item.Name ==
                     WindowInfo.LegacyReadoutName);
        }

        private static void RemoveStaleSections()
        {
            List<SectionModule> staleSections =
                SectionLibrary.CustomSections
                    .Where(
                        candidate =>
                            candidate != null &&
                            (candidate.Name ==
                                 PlaneInfo.SectionName ||
                             candidate.Abbreviation ==
                                 PlaneInfo.SectionAbbreviation ||
                             candidate.Name ==
                                 WindowInfo.SectionName ||
                             candidate.Abbreviation ==
                                 WindowInfo.SectionAbbreviation))
                    .ToList();

            foreach (SectionModule stale in staleSections)
            {
                CloseSectionWindows(stale);
                SectionLibrary.CustomSections.Remove(stale);
            }
        }

        private void Update()
        {
            if (!installed)
            {
                return;
            }

            if (Time.frameCount >=
                nextIntegrityCheckFrame)
            {
                nextIntegrityCheckFrame =
                    Time.frameCount +
                    IntegrityCheckIntervalFrames;

                bool objectsMissing =
                    planeReadout == null ||
                    windowReadout == null ||
                    planeSection == null ||
                    windowSection == null ||
                    !ReadoutLibrary.Readouts.Contains(
                        planeReadout) ||
                    !ReadoutLibrary.Readouts.Contains(
                        windowReadout) ||
                    !SectionLibrary.CustomSections.Contains(
                        planeSection) ||
                    !SectionLibrary.CustomSections.Contains(
                        windowSection);

                if (objectsMissing)
                {
                    Debug.LogWarning(
                        SuiteInfo.LogPrefix +
                        " KER runtime objects changed; " +
                        "repairing WIND and PLNE.");

                    InstallOrRepair();
                    return;
                }
            }

            LaunchWindowProcessor.Update();
            PlaneMatchProcessor.Update();
            RefreshVisibility(false);
        }

        private void RefreshVisibility(bool force)
        {
            bool resized = false;

            resized |=
                RefreshSectionVisibility(
                    windowSection,
                    LaunchWindowProcessor.HasValidTarget,
                    ref lastWindowButtonState,
                    force);

            resized |=
                RefreshSectionVisibility(
                    planeSection,
                    PlaneMatchProcessor.HasValidTarget,
                    ref lastPlaneButtonState,
                    force);

            if (resized)
            {
                RequestDisplayResize();
            }
        }

        private static bool RefreshSectionVisibility(
            SectionModule section,
            bool shouldShowButton,
            ref bool lastState,
            bool force)
        {
            if (section == null)
            {
                return false;
            }

            if (!force &&
                shouldShowButton == lastState)
            {
                return false;
            }

            lastState = shouldShowButton;
            section.showButton = shouldShowButton;

            if (!shouldShowButton)
            {
                CloseSectionWindows(section);
            }

            return true;
        }

        private static void CloseSectionWindows(
            SectionModule candidate)
        {
            if (candidate == null)
            {
                return;
            }

            try
            {
                candidate.IsVisible = false;
                candidate.IsEditorVisible = false;
                candidate.IsFloating = false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SuiteInfo.LogPrefix +
                    " section-window cleanup warning: " +
                    exception.Message);
            }
        }

        private static void RequestDisplayResize()
        {
            if (DisplayStack.Instance != null)
            {
                DisplayStack.Instance.RequestResize();
            }
        }

        private void RemoveRuntimeObjects()
        {
            RemoveSection(ref windowSection);
            RemoveSection(ref planeSection);
            RemoveReadout(ref windowReadout);
            RemoveReadout(ref planeReadout);
        }

        private static void RemoveSection(
            ref SectionModule section)
        {
            if (section == null)
            {
                return;
            }

            CloseSectionWindows(section);

            if (SectionLibrary.CustomSections != null)
            {
                SectionLibrary.CustomSections.Remove(
                    section);
            }

            section = null;
        }

        private static void RemoveReadout<T>(
            ref T readout)
            where T : ReadoutModule
        {
            if (readout == null)
            {
                return;
            }

            if (ReadoutLibrary.Readouts != null)
            {
                ReadoutLibrary.Readouts.Remove(
                    readout);
            }

            readout = null;
        }

        private void OnDestroy()
        {
            try
            {
                RemoveRuntimeObjects();
                RequestDisplayResize();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    SuiteInfo.LogPrefix +
                    " cleanup warning: " +
                    exception.Message);
            }
        }
    }
}
