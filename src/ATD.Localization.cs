// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
//
// Unofficial mod for Captain of Industry. Captain of Industry, MaFi Games, and
// related trademarks, code, and assets belong to MaFi Games. This repository is
// intended to contain only original mod code/configuration; if MaFi Games material
// is included by mistake, I intend to correct it promptly upon discovery or notice.
using Mafi.Localization;

namespace AutoTerrainDesignations
{
    /// <summary>
    /// Static localization fields for ATD. All LocStr fields are rebound by
    /// <see cref="CoI.AutoHelpers.Localization.ModTranslations.Apply"/> at renderer init state.
    /// </summary>
    internal static class AtdLocalization
    {
        /// <summary>
        /// Returns a <see cref="LocStrFormatted"/> with the ATD mod marker appended,
        /// for use as tooltip text.
        /// </summary>
        public static LocStrFormatted Tip(LocStr s) =>
            new LocStrFormatted(AutoTerrainDesignationsMod.Tt(s.TranslatedString));

        // ------------------------------------------------------------------ //
        // Common levels
        // ------------------------------------------------------------------ //
        public static LocStr LevelOff  = Loc.Str("common.level.off",  "Off",  "Common level setting label: off/disabled.");
        public static LocStr LevelLow  = Loc.Str("common.level.low",  "Low",  "Common level setting label: low.");
        public static LocStr LevelMed  = Loc.Str("common.level.med",  "Med",  "Common level setting label: medium.");
        public static LocStr LevelHigh = Loc.Str("common.level.high", "High", "Common level setting label: high.");
        public static LocStr LevelMax  = Loc.Str("common.level.max",  "Max",  "Common level setting label: maximum.");

        // ------------------------------------------------------------------ //
        // Terrain designation panel
        // ------------------------------------------------------------------ //
        public static LocStr DesigTitle =
            Loc.Str("panel.designations.title", "Terrain Designations", "Title of the terrain designations inspector panel.");
        public static LocStr DesigDescription =
            Loc.Str("panel.designations.description", "Create automatic terrain designations for this tower.", "Tooltip on the terrain designations panel title.");
        public static LocStr DesigCreateBtn =
            Loc.Str("panel.designations.create_button", "Create Designations", "Label on the Create Designations button.");
        public static LocStr DesigCreateTip =
            Loc.Str("panel.designations.create_tooltip", "Scan and place mining designations in this tower's area.", "Tooltip on the Create Designations button.");
        public static LocStr DesigDebrisTip =
            Loc.Str("panel.designations.debris_tooltip", "Designate all debris in the area for mining/removal. Overrides any forestry designations.", "Tooltip on the Debris button.");
        public static LocStr DesigClearTip =
            Loc.Str("panel.designations.clear_tooltip", "Clear all mining designations in this tower's area.", "Tooltip on the Clear button.");
        public static LocStr DesigOreFilterAuto =
            Loc.Str("panel.designations.ore_filter.auto", "Auto (useful -> debris -> dirt)", "Label for the automatic ore filter option in the ore picker.");
        public static LocStr DesigRampWidthLabel =
            Loc.Str("panel.designations.ramp_width.label", "Ramp width", "Label for the ramp width setting row.");
        public static LocStr DesigRampWidthTip =
            Loc.Str("panel.designations.ramp_width.tooltip", "Width of generated access ramps (0 = disable ramp).", "Tooltip for the ramp width setting.");
        public static LocStr DesigMaxLayersLabel =
            Loc.Str("panel.designations.max_layers.label", "Max layers to excavate", "Label for the max layers setting row.");
        public static LocStr DesigMaxLayersTip =
            Loc.Str("panel.designations.max_layers.tooltip", "Maximum layers to excavate from the surface. (\u221e = no limit.)", "Tooltip for the max layers setting.");
        public static LocStr DesigElevLimitLabel =
            Loc.Str("panel.designations.elevation_limit.label", "Elevation limit", "Label for the elevation limit setting row.");
        public static LocStr DesigElevLimitTip =
            Loc.Str("panel.designations.elevation_limit.tooltip", "Maximum (absolute) excavation depth (-\u221e = no limit.)", "Tooltip for the elevation limit setting.");
        public static LocStr DesigOrePurityLabel =
            Loc.Str("panel.designations.ore_purity.label", "Ore purity", "Label for the ore purity setting row.");
        public static LocStr DesigOrePurityTip =
            Loc.Str("panel.designations.ore_purity.tooltip",
                "How strictly the scan filters for ore quality.\n" +
                "Off: include all tiles, dig to full depth.\n" +
                "Low: exclude very sparse tiles, trim thin trailing ore at the bottom.\n" +
                "Med: moderate quality \u2014 skip tiles with heavy overburden or little ore.\n" +
                "High: only rich tiles with a clean ore column.\n" +
                "Max: near-pure ore only \u2014 strict on overburden, depth and ore density.",
                "Tooltip for the ore purity setting.");
        public static LocStr DesigCorridorClearanceLabel =
            Loc.Str("panel.designations.corridor_clearance.label", "Corridor clearance", "Label for the corridor clearance setting row.");
        public static LocStr DesigCorridorClearanceTip =
            Loc.Str("panel.designations.corridor_clearance.tooltip",
                "Minimum corridor width for connecting ore regions and enforcing passability.\n" +
                "0 = disabled (regions left separate, no corridors or hole-filling).\n" +
                "1 = 1-tile corridors (small and medium vehicles).\n" +
                "2 = 2-tile corridors (mega vehicles).\n",
                "Tooltip for the corridor clearance setting.");
        public static LocStr DesigScanningFilterLabel =
            Loc.Str("panel.designations.scanning_filter.label", "Scanning filter:", "Label for the scanning filter ore picker row.");
        public static LocStr DesigScanningFilterTip =
            Loc.Str("panel.designations.scanning_filter.tooltip", "Force the scan to target a specific product. None = useful products first, then debris, then dirt.", "Tooltip for the scanning filter ore picker.");
        public static LocStr DesigRampWarnFailed =
            Loc.Str("panel.designations.ramp_warning.failed", "Ramp generation failed \u2014 no valid path found.", "Warning icon tooltip when ramp generation failed.");
        public static LocStr DesigRampWarnTruncated =
            Loc.Str("panel.designations.ramp_warning.truncated", "Ramp placed but did not reach the surface \u2014 excavators may not be able to excavate.", "Warning icon tooltip when ramp was truncated.");
        public static LocStr DesigRampWarnNotAccessible =
            Loc.Str("panel.designations.ramp_warning.not_accessible", "Couldn't find a valid path from the tower to the generated ramp. Check for access problems.", "Warning icon tooltip when ramp is not accessible.");

        // ------------------------------------------------------------------ //
        // Ore composition panel
        // ------------------------------------------------------------------ //
        public static LocStr OreTitle =
            Loc.Str("panel.ore.title", "Ore Composition", "Title of the ore composition inspector panel.");
        public static LocStr OreDescription =
            Loc.Str("panel.ore.description", "Ore resources within this tower's current mining designations. (Does not account for potential landslides.)", "Tooltip on the ore composition panel title.");
        public static LocStr OrePromptScan =
            Loc.Str("panel.ore.prompt_scan", "Press \u21ba to scan ore composition.", "Prompt shown before a scan is run.");
        public static LocStr OreScanTip =
            Loc.Str("panel.ore.scan_tooltip", "Scan ore composition", "Tooltip on the scan/refresh button in the ore composition panel.");
        public static LocStr OreNoTower =
            Loc.Str("panel.ore.no_tower", "No tower selected.", "Message shown when no tower is selected in the ore panel.");
        public static LocStr OreNoMinableDesig =
            Loc.Str("panel.ore.no_minable_designations", "No minable designations found.", "Message shown when the scan finds no minable designations.");
        public static LocStr OrePrioritySelectedTipFmt =
            Loc.Str("panel.ore.priority_selected_tooltip", "Excavators set to prioritize {0}. Click to unset.", "Tooltip on a priority button when that product is already prioritized. {0} = colored product name.");
        public static LocStr OrePrioritySetTipFmt =
            Loc.Str("panel.ore.priority_set_tooltip", "Set all excavators to prioritize {0}.", "Tooltip on a priority button. {0} = colored product name.");

        // ------------------------------------------------------------------ //
        // Farming analysis panel
        // ------------------------------------------------------------------ //
        public static LocStr FarmingTitle =
            Loc.Str("panel.farming.title", "Farmland Preparation", "Title of the farmland preparation inspector panel.");
        public static LocStr FarmingDescription =
            Loc.Str("panel.farming.description", "Automates the preparation and final filling of flat level designations so their top layer becomes farmable.", "Tooltip on the farmland preparation panel title.");
        public static LocStr FarmingToggleLabel =
            Loc.Str("panel.farming.automation_toggle.label", "Farmland Preparation Automation", "Label on the farming automation toggle.");
        public static LocStr FarmingToggleTip =
            Loc.Str("panel.farming.automation_toggle.tooltip", "Prepare flat level designations for farmland by clearing unsuitable top material, then restoring the final fill orders.", "Tooltip on the farming automation toggle.");

        // ------------------------------------------------------------------ //
        // Toolbox items
        // ------------------------------------------------------------------ //
        public static LocStr CornerOuterTip =
            Loc.Str("toolbox.corner_outer.tooltip", "Corner (outer): place convex corner ramps.", "Tooltip on the outer corner toolbox item.");
        public static LocStr CornerInnerTip =
            Loc.Str("toolbox.corner_inner.tooltip", "Corner (inner): place concave corner ramps.", "Tooltip on the inner corner toolbox item.");

        // ------------------------------------------------------------------ //
        // Notifications
        // ------------------------------------------------------------------ //
        public static LocStr NotifRampFailed =
            Loc.Str("notification.ramp_access_failed", "[ATD] {entity} could not start an access ramp", "Notification: ramp generation failed. {entity} is substituted by the game.");
        public static LocStr NotifRampTruncated =
            Loc.Str("notification.ramp_access_truncated", "[ATD] {entity} could not fit a full access ramp", "Notification: ramp was truncated. {entity} is substituted by the game.");
        public static LocStr NotifRampNotAccessible =
            Loc.Str("notification.ramp_access_not_accessible", "[ATD] {entity} could not path to the ramp", "Notification: ramp not accessible. {entity} is substituted by the game.");
        public static LocStr NotifFarmingComplete =
            Loc.Str("notification.farming_complete", "[ATD] {entity} farming preparation and filling complete", "Notification: farming complete. {entity} is substituted by the game.");
        public static LocStr NotifExcavatorCompleted =
            Loc.Str("notification.excavator_completed", "[ATD] {entity} completed an excavator", "Notification: excavator built. {entity} is substituted by the game.");

        // ------------------------------------------------------------------ //
        // Farming session status strings
        // ------------------------------------------------------------------ //
        public static LocStr FarmingNoTower =
            Loc.Str("farming.no_tower", "[ATD Farming] No tower selected.", "Status: no tower selected.");
        public static LocStr FarmingDesigMgrUnavailable =
            Loc.Str("farming.designation_manager_unavailable", "[ATD Farming] Terrain designation manager is unavailable.", "Status: designation manager unavailable.");
        public static LocStr FarmingLevelingProtoUnavailable =
            Loc.Str("farming.leveling_proto_unavailable", "[ATD Farming] Leveling prototype is unavailable.", "Status: leveling proto unavailable.");
        public static LocStr FarmingNoStableEntityId =
            Loc.Str("farming.no_stable_entity_id", "[ATD Farming] Selected tower has no stable entity id.", "Status: tower has no stable id.");
        public static LocStr FarmingFillingWaitingAllTrackedReady =
            Loc.Str("farming.filling_waiting_all_tracked_ready", "[ATD Farming] Filling is waiting: all tracked tower designations must be ReadyForFilling or Done before tower dump rules can change.", "Status: filling waiting for all designations to be ready.");
        public static LocStr FarmingAutomationEnabled =
            Loc.Str("farming.automation_enabled", "[ATD Farming] Farming automation enabled.", "Status: farming automation enabled.");
        public static LocStr FarmingStage3Started =
            Loc.Str("farming.stage3_started", "[ATD Farming] Stage 3 preparation session started.", "Status: stage 3 started.");
        public static LocStr FarmingAlreadyDisabled =
            Loc.Str("farming.already_disabled", "[ATD Farming] Farming automation is already disabled for this tower.", "Status: farming already disabled.");
        public static LocStr FarmingRestoreAttemptedFmt =
            Loc.Str("farming.restore_attempted", "[ATD Farming] Restore attempted: restored={0}, failed={1}.", "Status after a restore attempt. {0}=restored count, {1}=failed count.");
        public static LocStr FarmingRestoredOriginsFmt =
            Loc.Str("farming.restored_origins", "[ATD Farming] Restored {0} Stage 3 origin(s) for this tower.", "Status after successful restore. {0}=count.");
        public static LocStr FarmingNoStage3Session =
            Loc.Str("farming.no_stage3_session", "[ATD Farming] No Stage 3 preparation session exists for this tower. Run preparation first.", "Status: no stage 3 session.");
        public static LocStr FarmingSessionAlreadyActive =
            Loc.Str("farming.session_already_active", "[ATD Farming] A preparation/filling session is already active for this tower.", "Status: session already active.");
        public static LocStr FarmingFillingWaitingPreparation =
            Loc.Str("farming.filling_waiting_preparation", "[ATD Farming] Filling is waiting: one or more tower designations still need leveling or preparation.", "Status: waiting for preparation.");
        public static LocStr FarmingStage4DesigMgrUnavailable =
            Loc.Str("farming.stage4_designation_manager_unavailable", "[ATD Farming] Stage 4 blocked: designation manager unavailable.", "Status: stage 4 blocked, no designation manager.");
        public static LocStr FarmingStage4LevelingProtoUnavailable =
            Loc.Str("farming.stage4_leveling_proto_unavailable", "[ATD Farming] Stage 4 blocked: leveling prototype unavailable.", "Status: stage 4 blocked, no leveling proto.");
        public static LocStr FarmingNoOriginsReady =
            Loc.Str("farming.no_origins_ready", "[ATD Farming] No origins are ready for filling/restoration. Run preparation first or wait for leveling/preparation to finish.", "Status: no origins ready for filling.");
        public static LocStr FarmingStage4WaitingVehiclesFmt =
            Loc.Str("farming.stage4_waiting_vehicles", "[ATD Farming] Stage 4 filling started: waiting briefly for {0} vehicle(s) to leave the fill area before committing fill designations.", "Status: waiting for vehicles. {0}=vehicle count.");
        public static LocStr FarmingStage4FailedRestoreFmt =
            Loc.Str("farming.stage4_failed_restore", "[ATD Farming] Stage 4 blocked: failed to restore {0} ready origin(s).", "Status: stage 4 blocked, restore failed. {0}=count.");
        public static LocStr FarmingStage4NoFillOrigins =
            Loc.Str("farming.stage4_no_fill_origins", "[ATD Farming] Stage 4 blocked: no fill origins could be activated.", "Status: stage 4 blocked, no fill origins.");
        public static LocStr FarmingStage4StartedFmt =
            Loc.Str("farming.stage4_started", "[ATD Farming] Stage 4 filling started: activated={0}, failed={1}, farmableProducts={2}.", "Status: stage 4 started. {0}=activated, {1}=failed, {2}=farmable product count.");
        public static LocStr FarmingAutomationOffPeriod =
            Loc.Str("farming.automation_off_period", "[ATD Farming] Farming automation: off.", "Status: automation off period.");
        public static LocStr FarmingAutomationOn =
            Loc.Str("farming.automation_on", "[ATD Farming] Farming automation: on", "Status: automation on.");
        public static LocStr FarmingAutomationOff =
            Loc.Str("farming.automation_off", "[ATD Farming] Farming automation: off", "Status: automation off.");
        public static LocStr FarmingSummaryCountsFmt =
            Loc.Str("farming.summary_counts", "  Active origins={0}, Analysis/Leveling={1}, Preparing={2}, ReadyForFilling={3}, Filling={4}, Done={5}, Blocked={6}", "Farming session summary line. All placeholders are counts.");
        public static LocStr FarmingTowerDumpRulesRestricted =
            Loc.Str("farming.tower_dump_rules_restricted", "  Tower dump rules are temporarily restricted to farmable products.", "Status line: dump rules restricted during filling.");
        public static LocStr FarmingMoreOriginsOmittedFmt =
            Loc.Str("farming.more_origins_omitted", "  ... {0} more origin(s) omitted.", "Status line indicating omitted origins. {0}=count.");
    }
}
