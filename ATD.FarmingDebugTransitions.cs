// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
//
// Unofficial mod for Captain of Industry. Captain of Industry, MaFi Games, and
// related trademarks, code, and assets belong to MaFi Games. This repository is
// intended to contain only original mod code/configuration; if MaFi Games material
// is included by mistake, I intend to correct it promptly upon discovery or notice.
// Auto Terrain Designations - Farming Preparation Debug Transitions
using System.Collections.Generic;
using Mafi;
using Mafi.Core.Terrain.Designation;

namespace AutoTerrainDesignations
{
    public static partial class AutoDepthDesignation
    {
        private sealed class FarmingDebugStoredDesignation
        {
            public DesignationData OriginalData { get; }
            public int TargetHeight { get; }

            public FarmingDebugStoredDesignation(DesignationData originalData, int targetHeight)
            {
                OriginalData = originalData;
                TargetHeight = targetHeight;
            }
        }

        private static readonly Dictionary<Tile2i, FarmingDebugStoredDesignation> s_farmingDebugStoredDesignations =
            new Dictionary<Tile2i, FarmingDebugStoredDesignation>();

        internal static string AnalyzeFarmingOriginForDebug(int x, int y)
        {
            if (!TryGetFarmingDebugContext(x, y, out Tile2i origin, out TerrainDesignation designation, out string error))
                return error;

            if (!TryGetFlatTargetHeight(designation.Data, out int targetHeight))
                return FormatFarmingDebugPrefix(origin) + "SkippedNonFlat: designation has mixed corner target heights.";

            FarmingAnalysisRow row = AnalyzeFarmingDesignation(origin, targetHeight, s_desigManager!.TerrainManager);
            return FormatFarmingDebugRow(row);
        }

        internal static string PrepareFarmingOriginForDebug(int x, int y)
        {
            if (!TryGetFarmingDebugContext(x, y, out Tile2i origin, out TerrainDesignation designation, out string error))
                return error;

            if (s_levelingProto == null)
                return "[ATD Farming] Leveling prototype is unavailable.";

            if (s_farmingDebugStoredDesignations.ContainsKey(origin))
                return FormatFarmingDebugPrefix(origin) + "This origin already has a stored Stage 2 preparation designation. Use atd_farming_restore_origin first.";

            if (!TryGetFlatTargetHeight(designation.Data, out int targetHeight))
                return FormatFarmingDebugPrefix(origin) + "SkippedNonFlat: designation has mixed corner target heights.";

            FarmingAnalysisRow row = AnalyzeFarmingDesignation(origin, targetHeight, s_desigManager!.TerrainManager);
            if (row.State != FarmingAnalysisState.NeedsPreparation)
                return FormatFarmingDebugPrefix(origin) + $"No preparation transition performed. Current state is {row.State}. {row.Detail}";

            if (!TryPlaceFarmingPreparationDesignation(origin, designation.Data, targetHeight))
                return FormatFarmingDebugPrefix(origin) + "Failed to place temporary preparation level designation.";

            return FormatFarmingDebugPrefix(origin) +
                $"Prepared: stored original target={targetHeight}, placed temporary LevelDesignator target={targetHeight - 1}.";
        }

        internal static string RestoreFarmingOriginForDebug(int x, int y)
        {
            if (s_desigManager == null)
                return "[ATD Farming] Terrain designation manager is unavailable.";

            if (s_levelingProto == null)
                return "[ATD Farming] Leveling prototype is unavailable.";

            Tile2i origin = TerrainDesignation.GetOrigin(new Tile2i(x, y));
            if (!s_farmingDebugStoredDesignations.TryGetValue(origin, out FarmingDebugStoredDesignation stored))
                return FormatFarmingDebugPrefix(origin) + "No stored Stage 2 original designation exists for this origin.";

            if (!s_desigManager.AddOrReplaceDesignation(s_levelingProto, stored.OriginalData))
                return FormatFarmingDebugPrefix(origin) + "Failed to restore original level designation. Stored data was kept.";

            s_farmingDebugStoredDesignations.Remove(origin);
            return FormatFarmingDebugPrefix(origin) + $"Restored original level designation target={stored.TargetHeight}.";
        }

        private static bool TryGetFarmingDebugContext(
            int x,
            int y,
            out Tile2i origin,
            out TerrainDesignation designation,
            out string error)
        {
            origin = TerrainDesignation.GetOrigin(new Tile2i(x, y));
            designation = default!;
            error = string.Empty;

            if (s_desigManager == null)
            {
                error = "[ATD Farming] Terrain designation manager is unavailable.";
                return false;
            }

            var designationAtOrigin = s_desigManager.GetDesignationAt(origin);
            if (!designationAtOrigin.HasValue)
            {
                error = FormatFarmingDebugPrefix(origin) + "No terrain designation exists at this origin.";
                return false;
            }

            designation = designationAtOrigin.Value;
            if (!IsLevelingDesignation(designation))
            {
                error = FormatFarmingDebugPrefix(origin) + $"Designation is {designation.Prototype.Id}, not a LevelDesignator.";
                return false;
            }

            return true;
        }

        private static string FormatFarmingDebugRow(FarmingAnalysisRow row)
        {
            string target = row.TargetHeight.HasValue ? row.TargetHeight.Value.ToString() : "-";
            return FormatFarmingDebugPrefix(row.Origin) +
                $"{row.State}: target={target}, surface={row.MinSurfaceHeight:F2}..{row.MaxSurfaceHeight:F2}, " +
                $"farmableMin={row.MinFarmableThicknessInBand:F2}, nonFarmCells={row.NonFarmableCells}. {row.Detail}";
        }

        private static string FormatFarmingDebugPrefix(Tile2i origin)
        {
            return $"[ATD Farming] ({origin.X},{origin.Y}) ";
        }

        private static bool TryPlaceFarmingPreparationDesignation(Tile2i origin, DesignationData originalData, int targetHeight)
        {
            if (s_desigManager == null || s_levelingProto == null)
                return false;

            int preparationHeight = targetHeight - 1;
            DesignationData preparationData = BuildFlatLevelDesignationData(origin, preparationHeight);

            s_farmingDebugStoredDesignations[origin] = new FarmingDebugStoredDesignation(originalData, targetHeight);
            if (s_desigManager.AddOrReplaceDesignation(s_levelingProto, preparationData))
                return true;

            s_farmingDebugStoredDesignations.Remove(origin);
            return false;
        }

        private static DesignationData BuildFlatLevelDesignationData(Tile2i origin, int targetHeight)
        {
            return new DesignationData(
                origin,
                new HeightTilesI(targetHeight),
                new HeightTilesI(targetHeight),
                new HeightTilesI(targetHeight),
                new HeightTilesI(targetHeight));
        }
    }
}
