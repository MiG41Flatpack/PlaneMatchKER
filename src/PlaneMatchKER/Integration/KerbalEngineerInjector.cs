// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 PlaneMatchKER contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KerbalEngineer.Flight;
using KerbalEngineer.Flight.Readouts;
using KerbalEngineer.Flight.Sections;
using PlaneMatchKER.Core;
using PlaneMatchKER.Readouts;
using UnityEngine;

namespace PlaneMatchKER.Integration
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class KerbalEngineerInjector : MonoBehaviour
    {
        private const string LogPrefix = "[PlaneMatchKER]";
        private const int IntegrityCheckIntervalFrames = 120;

        private SectionModule section;
        private PlaneMatchPanelReadout readout;
        private bool installed;
        private bool lastButtonState;
        private int nextIntegrityCheckFrame;

        private void Start()
        {
            StartCoroutine(InstallWhenReady());
        }

        private IEnumerator InstallWhenReady()
        {
            while (FlightEngineerCore.Instance == null ||
                   DisplayStack.Instance == null)
            {
                yield return null;
            }

            // KER loads its saved section library during startup. Inject after
            // that load so a stale serialized PLNE section cannot replace us.
            yield return null;
            yield return null;

            InstallOrRepair();
        }

        private void InstallOrRepair()
        {
            try
            {
                RemoveRuntimeObjects();
                RegisterReadout();
                RemoveStaleSections();

                section = new SectionModule
                {
                    Name = ModInfo.SectionName,
                    Abbreviation = ModInfo.SectionAbbreviation,
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

                SectionLibrary.CustomSections.Add(section);

                readout.Reset();
                installed = true;
                nextIntegrityCheckFrame =
                    Time.frameCount +
                    IntegrityCheckIntervalFrames;

                RefreshTargetVisibility(true);
                RequestDisplayResize();

                Debug.Log(
                    LogPrefix +
                    " " +
                    ModInfo.Version +
                    " registered PLNE section in KER.");
            }
            catch (Exception exception)
            {
                installed = false;

                Debug.LogError(
                    LogPrefix +
                    " installation failed: " +
                    exception);
            }
        }

        private void RegisterReadout()
        {
            ReadoutLibrary.Readouts.RemoveAll(
                item =>
                    item != null &&
                    (item.GetType() ==
                         typeof(PlaneMatchPanelReadout) ||
                     item.Name ==
                         "Plane Match Panel" ||
                     item.Name ==
                         ModInfo.ReadoutName));

            readout = new PlaneMatchPanelReadout();
            ReadoutLibrary.Readouts.Add(readout);
        }

        private void RemoveStaleSections()
        {
            List<SectionModule> staleSections =
                SectionLibrary.CustomSections
                    .Where(
                        candidate =>
                            candidate != null &&
                            (candidate.Name ==
                                 ModInfo.SectionName ||
                             candidate.Abbreviation ==
                                 ModInfo.SectionAbbreviation))
                    .ToList();

            foreach (SectionModule stale in staleSections)
            {
                CloseSectionWindows(stale);
                SectionLibrary.CustomSections.Remove(stale);
            }
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
                    LogPrefix +
                    " section-window cleanup warning: " +
                    exception.Message);
            }
        }

        private void Update()
        {
            if (!installed)
            {
                return;
            }

            if (Time.frameCount >= nextIntegrityCheckFrame)
            {
                nextIntegrityCheckFrame =
                    Time.frameCount +
                    IntegrityCheckIntervalFrames;

                bool readoutMissing =
                    readout == null ||
                    !ReadoutLibrary.Readouts.Contains(readout);

                bool sectionMissing =
                    section == null ||
                    !SectionLibrary.CustomSections.Contains(section);

                if (readoutMissing || sectionMissing)
                {
                    Debug.LogWarning(
                        LogPrefix +
                        " KER runtime objects changed; repairing PLNE.");

                    InstallOrRepair();
                    return;
                }
            }

            PlaneMatchProcessor.Update();
            RefreshTargetVisibility(false);
        }

        private void RefreshTargetVisibility(bool force)
        {
            if (section == null)
            {
                return;
            }

            bool shouldShowButton =
                PlaneMatchProcessor.HasValidTarget;

            if (!force &&
                shouldShowButton == lastButtonState)
            {
                return;
            }

            lastButtonState = shouldShowButton;
            section.showButton = shouldShowButton;

            if (!shouldShowButton)
            {
                CloseSectionWindows(section);
            }

            RequestDisplayResize();
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
            if (section != null)
            {
                CloseSectionWindows(section);

                if (SectionLibrary.CustomSections != null)
                {
                    SectionLibrary.CustomSections.Remove(section);
                }

                section = null;
            }

            if (readout != null)
            {
                if (ReadoutLibrary.Readouts != null)
                {
                    ReadoutLibrary.Readouts.Remove(readout);
                }

                readout = null;
            }
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
                    LogPrefix +
                    " cleanup warning: " +
                    exception.Message);
            }
        }
    }
}
