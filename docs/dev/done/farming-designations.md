# Farming Designations — Architecture Reference

## Feature summary

Farming Designations automates the preparation and topsoil filling of flat level designations so the resulting surface is farmable. It runs as a background per-tower session driven by `AutoTerrainDesignationsTicker`. The inspector panel is a control and status surface only; closing it does not pause automation.

---

## Source files

| File | Role |
|---|---|
| `ATD.FarmingAnalysis.cs` | Read-only per-origin analysis; produces `FarmingAnalysisRow` |
| `ATD.FarmingPreparationSession.cs` | Session data structures, tick loop, state transitions, save hook |
| `ATD.FarmingFillActivation.cs` | Stage 4 — filling: dump rule manipulation, vehicle clear-out, fill designation activation |
| `ATD.FarmingAccess.cs` | Access ramp placement (preparation and filling phases), result caching |
| `ATD.FarmingDebugTransitions.cs` | Debug console commands (`atd_farming_*`) for single-origin testing |
| `ATD.FarmingAnalysisPanel.cs` | UI: injects the Farmland Preparation panel into the mine tower inspector |

All farming code lives inside the `AutoDepthDesignation` partial class (`namespace AutoTerrainDesignations`), except the session data classes and the panel helper.

---

## State model

There are two distinct state enums. Do not confuse them.

### `FarmingAnalysisState` — read-only terrain snapshot

Produced fresh on every analysis pass from live terrain and designation data.
Never stored in the session.

| Value | Meaning |
|---|---|
| `Done` | Surface is at target height and the topsoil band is fully farmable |
| `NeedsLeveling` | Surface is at or above target; farmable band is clear; keep the level designation active |
| `ReadyForFilling` | Preparation cleared the band; topsoil fill can begin |
| `NeedsPreparation` | Non-farmable material exists in the future topsoil band, or surface is below `targetHeight - 1` |
| `SkippedNonFlat` | Level designation has unequal corner heights; not captured |

### `FarmingOriginPhase` — per-origin session state

Persisted in `FarmingOriginSession` across ticks.

| Value | Meaning |
|---|---|
| `AnalysisLeveling` | Captured; terrain analysis says `NeedsLeveling`; waiting for vanilla leveling |
| `Preparing` | Temporary preparation designation placed; waiting for excavators |
| `ReadyForFilling` | Preparation done; level designation hidden; queued for tower-level filling |
| `Filling` | Fill designation is active; waiting for trucks |
| `Done` | Topsoil band validated as farmable; origin is complete |
| `Blocked` | Progress failed (missing designation manager, placement failed, etc.) |

---

## "Flat" designation definition

Only level designations where all four `DesignationData` corner heights are equal (within `FARMING_HEIGHT_EPSILON = 0.05f`) are captured. Non-flat designations are analyzed as `SkippedNonFlat` and never enter a session.

---

## Topsoil band

The target band is the 1-tile layer from `targetHeight - 1` to `targetHeight`.

Analysis walks each of the 16 cells in the 4×4 designation tile and checks:

- `farmableThickness` in that band (from `TerrainMaterialProto.IsFarmable`)
- `nonFarmableThickness` — any positive value > `FARMING_HEIGHT_EPSILON` marks the cell as non-farmable

Thresholds:

```csharp
const float FARMING_HEIGHT_EPSILON   = 0.05f;
const float MIN_FARMABLE_THICKNESS   = 0.9003906f;   // from FarmFertileGroundValidator
```

An origin is `Done` when: surface equals `targetHeight` (within epsilon) **and**
`minFarmableThickness >= MIN_FARMABLE_THICKNESS` across all 16 cells.

---

## Farmable dump products

ATD discovers the set of dumpable farmable products dynamically at runtime:

```
LooseProductProto where LooseProductProto.TerrainMaterial.Value.IsFarmable == true
```

This handles dirt variants, compost, and any future or modded farmable materials without hard-coding names. The set is used to restrict the tower's `DumpableProducts` during filling.

---

## "Hide until filling" pattern

When an origin reaches `ReadyForFilling` or is already `Done`, ATD removes its
visible level designation from the terrain manager:

```csharp
s_desigManager.RemoveDesignation(originState.Origin);
originState.IsHiddenUntilFilling = true;
```

The original `DesignationData` is still stored in `FarmingOriginSession`. When the tower enters the filling phase, the original level designations are restored for all queued origins in one pass.

This ensures the game's own fill-completion detection can see the level
designations and mark them done after trucks finish.

---

## Tower-level filling (Stage 4)

Filling is tower-scoped, not per-origin, because dump rule ownership must be
held at tower level.

Steps when filling begins for a tower:

1. **Vehicle clear-out** — all vehicles on the future fill area are parked to the
   tower via `IParkAndWaitJobFactory`.  The session waits until the area is clear
   (or treats a vehicle as "stuck" after it fails to move, allowing it to be ignored).
2. **Dump rule snapshot** — the tower's current `DumpableProducts` set is recorded.
3. **Dump rule restriction** — tower is set to only accept farmable products.
4. **Fill designation activation** — stored original level designations are restored
   for all `ReadyForFilling` origins.
5. **Monitoring** — each tick re-analyses origins; `Done` ones clear, `Blocked`
   ones are logged.  A stabilization window (`FARMING_FILLING_STABILIZATION_SECONDS`)
   holds dump rules active briefly after all origins reach `Done` to let final
   trucks finish.
6. **Dump rule restore** — original dump rules are reinstated; session ends.

ATD **never** reads or modifies `ITerrainDumpingManager.ProductsAllowedToDump` (global dump rules). Only `MineTower.DumpableProducts` is touched.

---

## Access ramps

`ATD.FarmingAccess.cs` reuses the existing `CreateAccessRamp` path-finding
infrastructure.

- A **preparation** access ramp uses `MiningDesignator` proto.
- A **filling** access ramp uses `DumpingDesignator` proto.
- Results are cached per work-key (a hash of current work origin set and heights)
  and re-checked every `FARMING_ACCESS_RECHECK_TICKS = 10` ticks.
- Stale ramp designations are removed when there is no more work in that phase.
- Ramp origins are tracked in `FarmingPreparationSession.PreparationAccessRampOrigins`
  and `FillingAccessRampOrigins`.

---

## Save / load behavior

ATD does **not** persist session state. On save:

1. A save-start hook iterates active sessions and restores all temporary
   preparation designations to their original level designations.
2. `session.Active` is set to `session.Enabled` (i.e. paused if the toggle is on,
   fully off if it was already off).
3. After the save finishes, the ticker re-bootstraps sessions from the current
   visible state.

This makes farming tolerant of partial progress, player edits between sessions, and save migrations. The cost is that a running session must redo preparation analysis after every reload.

---

## Debug console commands

Defined in `ATD.FarmingDebugTransitions.cs`:

| Command | Effect |
|---|---|
| `atd_farming_analyze_origin x y` | Print Stage 1 analysis for the designation at (x, y) |
| `atd_farming_prepare_origin x y` | If `NeedsPreparation`, place temporary preparation designation |
| `atd_farming_restore_origin x y` | Restore original designation stored by the prepare command |
| `atd_farming_analyze_tower` | Print analysis for all designations of the currently selected tower |
| `atd_farming_dump_all` | Print full session state for every mine tower |

---

## Key Mafi API facts confirmed during development

- `TerrainDesignationsManager.AddOrReplaceDesignation(proto, data)` and
  `RemoveDesignation(origin)` are safe to call mid-game and produce the expected
  visual and pathfinding results.
- `MineTower.CanAcceptDumpOf(product)` checks only the tower's own
  `DumpableProducts` set — not the global `ITerrainDumpingManager`.
- `LooseProductProto.TerrainMaterial` is `Option<TerrainMaterialProto>`;
  `.Value.IsFarmable` is the correct farmability check.
- `IAreaManagingTower.ManagedDesignations` and `MineTower.ManagedDumpingDesignations`
  are safe to enumerate during a tick.
- `FarmFertileGroundValidator` uses `0.9` as the minimum farmable thickness
  threshold; ATD mirrors this as `MIN_FARMABLE_THICKNESS = 0.9003906f`.
