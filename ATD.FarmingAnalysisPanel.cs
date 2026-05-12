// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
//
// Unofficial mod for Captain of Industry. Captain of Industry, MaFi Games, and
// related trademarks, code, and assets belong to MaFi Games. This repository is
// intended to contain only original mod code/configuration; if MaFi Games material
// is included by mistake, I intend to correct it promptly upon discovery or notice.
// Auto Terrain Designations - Farming Preparation Analysis Panel
using System;
using System.Reflection;
using Mafi;
using Mafi.Core.Buildings.Towers;
using Mafi.Localization;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;

namespace AutoTerrainDesignations
{
    internal static class FarmingAnalysisPanel
    {
        private static readonly System.Collections.Generic.Dictionary<object, Action> s_resetContentCallbacks =
            new System.Collections.Generic.Dictionary<object, Action>();

        internal static void ResetContent(object inspectorInstance)
        {
            if (s_resetContentCallbacks.TryGetValue(inspectorInstance, out Action cb))
            {
                try { cb(); } catch { }
            }
        }

        internal static void Inject(Column mainBody, PropertyInfo entityProp, object inspector)
        {
            try
            {
                AutoDepthDesignation.EnsureFarmingAutomationDefaultEnabledForTower(
                    entityProp.GetValue(inspector) as IAreaManagingTower);
                var contentCol = new Column(2.pt());
                var actionLabel = new Label()
                    .FontSize(12)
                    .Color(Theme.InactiveColor);
                var analysisLabel = new Label(new LocStrFormatted("Press \u21ba to analyze farming prep."))
                    .FontSize(12)
                    .Color(Theme.InactiveColor);
                var statusLabel = new Label()
                    .FontSize(12)
                    .Color(Theme.InactiveColor)
                    .ObserveValue(() =>
                    {
                        var tower = entityProp.GetValue(inspector) as IAreaManagingTower;
                        return new LocStrFormatted(AutoDepthDesignation.FormatFarmingPreparationPanelStatusForTower(tower));
                    });
                var automationToggle = new Toggle(standalone: true)
                    .Label(new LocStrFormatted("Farming automation"))
                    .ObserveValue(() =>
                    {
                        var tower = entityProp.GetValue(inspector) as IAreaManagingTower;
                        return AutoDepthDesignation.IsFarmingAutomationEnabledForTower(tower);
                    })
                    .OnValueChanged((Action<bool>)delegate(bool isOn)
                    {
                        var tower = entityProp.GetValue(inspector) as IAreaManagingTower;
                        string result = AutoDepthDesignation.SetFarmingAutomationEnabledForTower(tower, isOn);
                        PopulateContent(actionLabel, analysisLabel, statusLabel, tower, result);
                    })
                    .Tooltip(new LocStrFormatted(AutoTerrainDesignationsMod.Tt("Enable or disable all farming preparation/filling automation for this tower.")));

                contentCol.Add(automationToggle);
                contentCol.Add(actionLabel);
                contentCol.Add(analysisLabel);
                contentCol.Add(statusLabel);

                s_resetContentCallbacks[inspector] = (Action)delegate
                {
                    AutoDepthDesignation.EnsureFarmingAutomationDefaultEnabledForTower(
                        entityProp.GetValue(inspector) as IAreaManagingTower);
                    actionLabel.Value(LocStrFormatted.Empty);
                    automationToggle.Value(AutoDepthDesignation.IsFarmingAutomationEnabledForTower(
                        entityProp.GetValue(inspector) as IAreaManagingTower));
                    analysisLabel.Value(new LocStrFormatted("Press \u21ba to analyze farming prep."));
                    statusLabel.Value(new LocStrFormatted(
                        AutoDepthDesignation.FormatFarmingPreparationPanelStatusForTower(entityProp.GetValue(inspector) as IAreaManagingTower)));
                };

                var panel = new PanelWithHeader()
                    .Title(
                        new LocStrFormatted("Farming Prep Analysis"),
                        new LocStrFormatted($"Farming preparation/filling automation for flat level designations. [{AutoTerrainDesignationsMod.ModMarker}]"));
                panel.Collapsed(false);

                panel.Header.Add(new ButtonIcon(
                    Button.General,
                    "Assets/Unity/UserInterface/General/Repeat.svg",
                    (Action)delegate
                    {
                        var tower = entityProp.GetValue(inspector) as IAreaManagingTower;
                        PopulateContent(actionLabel, analysisLabel, statusLabel, tower);
                    })
                    .Compact()
                    .IconSize(14.px())
                    .MarginLeft(4.pt())
                    .Tooltip(new LocStrFormatted(AutoTerrainDesignationsMod.Tt("Analyze flat leveling designations for farming preparation. Read-only."))));

                panel.BodyAdd(contentCol);
                mainBody.InsertAt(2, panel);
            }
            catch (Exception ex)
            {
                Log.Warning($"[ATD] FarmingAnalysisPanel.Inject EXCEPTION: {ex}");
            }
        }

        private static void PopulateContent(
            Label actionLabel,
            Label analysisLabel,
            Label statusLabel,
            IAreaManagingTower? tower,
            string? actionResult = null)
        {
            AutoDepthDesignation.EnsureFarmingAutomationDefaultEnabledForTower(tower);
            actionLabel.Value(string.IsNullOrEmpty(actionResult)
                ? LocStrFormatted.Empty
                : new LocStrFormatted(actionResult));

            string report = AutoDepthDesignation.FormatFarmingAnalysisForTower(tower);
            analysisLabel.Value(new LocStrFormatted(report));

            string status = AutoDepthDesignation.FormatFarmingPreparationPanelStatusForTower(tower);
            statusLabel.Value(new LocStrFormatted(status));
        }
    }
}
