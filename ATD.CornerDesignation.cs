// Auto Terrain Designations
// Copyright (c) 2026 Kayser
// Licensed under the MIT License.
//
// Unofficial mod for Captain of Industry. Captain of Industry, MaFi Games, and
// related trademarks, code, and assets belong to MaFi Games. This repository is
// intended to contain only original mod code/configuration; if MaFi Games material
// is included by mistake, I intend to correct it promptly upon discovery or notice.
// Auto Terrain Designations - Corner Designation Placement
using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Core.Terrain;
using Mafi.Core.Terrain.Designation;
using Mafi.Unity.InputControl;
using UnityEngine;

namespace AutoTerrainDesignations
{
    public static partial class AutoDepthDesignation
    {
        // One corner of the 4x4 tile raised (+1) or lowered (-1) relative to the other three.
        // Rotation index (0-3) encodes which corner: 0=Origin(NW), 1=PlusX(NE), 2=PlusXY(SE), 3=PlusY(SW).
        internal enum CornerVariant
        {
            // Outer: one corner at base+1
            OriginHigh  = 0,
            PlusXHigh   = 1,
            PlusXyHigh  = 2,
            PlusYHigh   = 3,
            // Inner: one corner at base-1
            OriginLow   = 4,
            PlusXLow    = 5,
            PlusXyLow   = 6,
            PlusYLow    = 7,
        }

        private static bool s_cornerModeActive;
        private static CornerVariant s_activeCornerVariant;
        private static TerrainCursor? s_terrainCursor;

        // AOE drag state.
        private static bool s_dragging;
        private static Tile2i s_dragOrigin;   // snap-aligned tile where LMB went down
        private static int s_dragBaseHeight;  // surface height + bias at drag start

        // True while the player has the game's mining designation tool active (M-mode).
        private static bool s_miningToolActive;
        // The live MiningDesignationController instance (needed to read m_heightBias).
        private static object? s_miningController;
        private static System.Reflection.FieldInfo? s_heightBiasField;

        // Tiles where ATD just placed a corner designation — protected from one subsequent
        // overwrite by the game's async placement command.
        private static readonly HashSet<Tile2i> s_protectedCornerTiles = new HashSet<Tile2i>();

        internal static void InitializeCornerMode(TerrainCursor? terrainCursor)
        {
            s_terrainCursor = terrainCursor;
            s_cornerModeActive = false;
            s_activeCornerVariant = CornerVariant.OriginHigh;
        }

        // Called every frame from AutoTerrainDesignationsTicker.Update().
        internal static void HandleCornerModeInput()
        {
            // K: only activates when the player is in M-mode (mining designation tool active).
            // If already in corner mode, K toggles inner/outer regardless.
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (s_cornerModeActive)
                    s_activeCornerVariant = ToggleCornerType(s_activeCornerVariant);
                else if (s_miningToolActive)
                    EnterCornerMode();
                return;
            }

            if (!s_cornerModeActive)
                return;

            // R: rotate among the 4 orientations within the current type.
            if (Input.GetKeyDown(KeyCode.R))
            {
                s_activeCornerVariant = RotateCornerVariant(s_activeCornerVariant);
                return;
            }

            // LMB down: start drag.
            if (Input.GetMouseButtonDown(0))
            {
                TryBeginDrag();
                return;
            }

            // LMB up: commit fill.
            if (s_dragging && Input.GetMouseButtonUp(0))
            {
                TryCommitDrag();
                return;
            }
        }

        // Called every frame from AutoTerrainDesignationsTicker.OnGUI() — draws the mode status bar.
        internal static void DrawCornerModeHud()
        {
            if (!s_cornerModeActive)
                return;

            string typeStr = IsOuterVariant(s_activeCornerVariant) ? "Outer" : "Inner";
            string rotStr  = GetCornerRotationLabel(s_activeCornerVariant);
            int bias       = GetHeightBias();
            string biasStr = bias == 0 ? "" : (bias > 0 ? $" +{bias}" : $" {bias}");

            string msg = $"[ATD] Corner: {typeStr} {rotStr}{biasStr}  |  K: toggle inner/outer  |  R: rotate  |  Q/E: height  |  LMB drag: fill area  |  exit M-mode to cancel";

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };
            // Semi-transparent background so the text is readable on any terrain.
            GUI.Box(new Rect(6, Screen.height - 44, Screen.width - 12, 32), GUIContent.none);
            GUI.Label(new Rect(10, Screen.height - 42, Screen.width - 20, 30), msg, style);
        }

        // -- Harmony patch handlers --

        // Postfix on TerrainDesignationController.Activate — sets s_miningToolActive when the
        // player activates the Mining designation tool specifically.
        public static void DesignationToolActivatePostfix(object __instance)
        {
            if (__instance.GetType().Name == "MiningDesignationController")
            {
                s_miningToolActive = true;
                s_miningController = __instance;
                // Resolve the height-bias field once and cache it.
                // m_heightBias is declared on the base class TerrainDesignationController,
                // so we must walk up the hierarchy — GetField only searches the declared type.
                if (s_heightBiasField == null)
                {
                    Type? t = __instance.GetType();
                    while (t != null && s_heightBiasField == null)
                    {
                        s_heightBiasField = t.GetField("m_heightBias",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
                        t = t.BaseType;
                    }
                }
                LogDebug("[ATD] Mining designation tool activated.");
            }
        }

        // Postfix on TerrainDesignationController.Deactivate — clears s_miningToolActive and
        // exits corner mode if the player leaves mining designation mode.
        public static void DesignationToolDeactivatePostfix(object __instance)
        {
            if (__instance.GetType().Name == "MiningDesignationController")
            {
                s_miningToolActive = false;
                s_miningController = null;
                if (s_cornerModeActive)
                    ExitCornerMode();
                LogDebug("[ATD] Mining designation tool deactivated.");
            }
        }

        // Prefix on TerrainDesignationsManager.AddOrReplaceDesignation — suppresses the game's
        // async placement command when it would overwrite a corner ATD just placed.
        // Protection is one-shot: consumed on the first suppressed call per tile.
        public static bool AddOrReplaceDesignationPrefix(DesignationData data, ref bool __result)
        {
            if (s_protectedCornerTiles.Contains(data.OriginTile))
            {
                s_protectedCornerTiles.Remove(data.OriginTile);
                __result = false;
                return false; // Skip original
            }
            return true; // Run original
        }

        // -- Shape helpers --

        /// <summary>
        /// Builds DesignationData for a corner shape. All 4 corners are at <paramref name="baseHeight"/>
        /// except the one corner indicated by <paramref name="variant"/>, which is ±1.
        /// </summary>
        internal static DesignationData BuildCornerDesignationData(
            Tile2i origin, int baseHeight, CornerVariant variant)
        {
            int nw = baseHeight, ne = baseHeight, se = baseHeight, sw = baseHeight;
            int delta = IsOuterVariant(variant) ? +1 : -1;

            // Rotation index: 0=Origin(NW), 1=PlusX(NE), 2=PlusXY(SE), 3=PlusY(SW)
            switch ((int)variant % 4)
            {
                case 0: nw += delta; break;
                case 1: ne += delta; break;
                case 2: se += delta; break;
                case 3: sw += delta; break;
            }

            // Constructor order: (origin, NW/origin, NE/plusX, SE/plusXy, SW/plusY)
            return new DesignationData(origin,
                new HeightTilesI(nw), new HeightTilesI(ne),
                new HeightTilesI(se), new HeightTilesI(sw));
        }

        // -- Private helpers --

        private static void EnterCornerMode()
        {
            s_cornerModeActive = true;
            s_activeCornerVariant = CornerVariant.OriginHigh;
            LogDebug("[ATD] Corner designation mode entered.");
        }

        private static void ExitCornerMode()
        {
            s_cornerModeActive = false;
            s_dragging = false;
            s_protectedCornerTiles.Clear();
            LogDebug("[ATD] Corner designation mode exited.");
        }

        private static void TryBeginDrag()
        {
            if (s_terrainCursor == null) return;
            if (!s_terrainCursor.TryComputeTerrainPosition(Input.mousePosition, out Tile3f pos3f)) return;

            TerrainManager terrMgr = s_desigManager!.TerrainManager;
            s_dragOrigin     = SnapToDesignationGrid(pos3f.Xy.Tile2i);
            s_dragBaseHeight = GetSurfaceHeight(terrMgr, s_dragOrigin) + GetHeightBias();
            s_dragging       = true;
        }

        private static void TryCommitDrag()
        {
            s_dragging = false;
            if (s_terrainCursor == null || s_desigManager == null) return;
            if (!s_terrainCursor.TryComputeTerrainPosition(Input.mousePosition, out Tile3f pos3f)) return;

            Tile2i end = SnapToDesignationGrid(pos3f.Xy.Tile2i);

            int minX = Math.Min(s_dragOrigin.X, end.X);
            int maxX = Math.Max(s_dragOrigin.X, end.X);
            int minY = Math.Min(s_dragOrigin.Y, end.Y);
            int maxY = Math.Max(s_dragOrigin.Y, end.Y);

            // Checkerboard parity is based on the drag-origin grid cell.
            int originGridX = s_dragOrigin.X / 4;
            int originGridY = s_dragOrigin.Y / 4;
            int originParity = ((originGridX + originGridY) % 2 + 2) % 2;

            CornerVariant complement = CheckerboardComplement(s_activeCornerVariant);

            for (int gx = minX; gx <= maxX; gx += 4)
            {
                for (int gy = minY; gy <= maxY; gy += 4)
                {
                    int gridX = gx / 4;
                    int gridY = gy / 4;
                    int parity = ((gridX + gridY) % 2 + 2) % 2; // always non-negative
                    bool isSameParity = (parity == originParity);

                    CornerVariant v = (parity == originParity) ? s_activeCornerVariant : complement;

                    // Diagonal slope: height varies along one diagonal axis depending on which
                    // corner is the elevated one of the checkerboard pair.
                    // outerRot = the rotation index of the OUTER corner in this pair
                    //   (for outer variants that's their own rot; for inner, their complement's rot).
                    int rot = (int)s_activeCornerVariant % 4;
                    int outerRot = IsOuterVariant(s_activeCornerVariant) ? rot : (rot + 2) % 4;

                    int deltaGridX = gridX - originGridX;
                    int deltaGridY = gridY - originGridY;

                    // slopeSum encodes the uphill axis for the given corner direction.
                    int slopeSum;
                    switch (outerRot)
                    {
                        case 0:  slopeSum =  deltaGridX + deltaGridY; break; // uphill NW
                        case 1:  slopeSum =  deltaGridY - deltaGridX; break; // uphill NE
                        case 2:  slopeSum = -(deltaGridX + deltaGridY); break; // uphill SE
                        default: slopeSum =  deltaGridX - deltaGridY; break; // uphill SW
                    }

                    // Same-parity sum is always even → exact integer division.
                    // Complement sum is always odd → use ceil or floor to avoid C# truncation error.
                    //   Outer origin: ceil(-sum/2) = -(sum-1)/2
                    //   Inner origin: floor(-sum/2) = -(sum+1)/2
                    int zOffset;
                    if (isSameParity)
                        zOffset = -(slopeSum / 2);
                    else if (IsOuterVariant(s_activeCornerVariant))
                        zOffset = -(slopeSum - 1) / 2;
                    else
                        zOffset = -(slopeSum + 1) / 2;

                    PlaceCornerAt(new Tile2i(gx, gy), s_dragBaseHeight + zOffset, v);
                }
            }
        }

        private static void PlaceCornerAt(Tile2i origin, int baseHeight, CornerVariant variant)
        {
            if (s_desigManager == null || s_miningProto == null) return;

            DesignationData data = BuildCornerDesignationData(origin, baseHeight, variant);
            s_protectedCornerTiles.Remove(origin);
            bool placed = s_desigManager.AddOrReplaceDesignation(s_miningProto, data);
            if (placed)
            {
                s_protectedCornerTiles.Add(origin);
                LogDebug($"[ATD] Placed {variant} corner at ({origin.X},{origin.Y}).");
            }
        }

        /// <summary>
        /// Checkerboard complement: rotate 180° and flip outer↔inner.
        /// E.g. NW-high → SE-low; both share all four edge heights at baseHeight.
        /// </summary>
        private static CornerVariant CheckerboardComplement(CornerVariant v)
        {
            int rot        = (int)v % 4;
            int typeOffset = (int)v >= 4 ? 0 : 4; // flip outer↔inner
            int newRot     = (rot + 2) % 4;        // rotate 180°
            return (CornerVariant)(typeOffset + newRot);
        }

        private static Tile2i SnapToDesignationGrid(Tile2i t)
        {
            // Designation origins must be on a 4-tile boundary.
            // Use floor division so negative coords also align correctly.
            int x = (int)Math.Floor(t.X / 4.0) * 4;
            int y = (int)Math.Floor(t.Y / 4.0) * 4;
            return new Tile2i(x, y);
        }

        private static int GetHeightBias()
        {
            if (s_miningController == null || s_heightBiasField == null)
                return 0;
            try
            {
                object? boxed = s_heightBiasField.GetValue(s_miningController);
                if (boxed is ThicknessTilesI bias)
                    return bias.Value;
            }
            catch { }
            return 0;
        }

        private static CornerVariant RotateCornerVariant(CornerVariant v)
        {
            int typeOffset = (int)v / 4 * 4; // 0 for outer, 4 for inner
            int rot        = (int)v % 4;
            return (CornerVariant)(typeOffset + (rot + 1) % 4);
        }

        private static CornerVariant ToggleCornerType(CornerVariant v)
        {
            int rot        = (int)v % 4;
            int typeOffset = (int)v >= 4 ? 0 : 4; // flip outer↔inner
            return (CornerVariant)(typeOffset + rot);
        }

        private static bool IsOuterVariant(CornerVariant v) => (int)v < 4;

        private static string GetCornerRotationLabel(CornerVariant v)
        {
            switch ((int)v % 4)
            {
                case 0: return "NW";
                case 1: return "NE";
                case 2: return "SE";
                case 3: return "SW";
                default: return "?";
            }
        }
    }
}
