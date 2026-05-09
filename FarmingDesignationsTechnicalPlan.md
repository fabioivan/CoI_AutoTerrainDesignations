# Farming Designations Technical Plan

## Goal

Let a mine tower run in a farming preparation mode where flat leveling designations are temporarily transformed into a multi-phase workflow:

1. Analyze flat leveling designations and let already-farmable terrain level normally when that is enough.
2. Prepare infertile terrain one height below the requested farm level, mining out non-farmable material.
3. Restore the original flat leveling designation and restrict tower dumping so the top layer is farmable material.

The high-level idea is workable, but it needs a few guardrails because the game distinguishes terrain designation type, farmable material thickness, tower dump permissions, and save serialization.

The intended robustness model is re-analysis, not exact phase persistence. On load or manual resume, ATD should restore/rely on visible canonical level designations and run a fresh analysis against current terrain and current player edits.

## Mafi API Assumptions Checked

- Leveling, dumping, and mining designations are all `TerrainDesignation` instances with a `TerrainDesignationProto` and `DesignationData`.
- Existing ATD already resolves `MiningDesignator`, `DumpingDesignator`, and `LevelDesignator` protos in `AutoDepthDesignation.Initialize`.
- `DesignationData` stores one origin tile plus four corner target heights:
  - `OriginTargetHeight`
  - `PlusXTargetHeight`
  - `PlusXyTargetHeight`
  - `PlusYTargetHeight`
- `TerrainDesignationsManager.AddOrReplaceDesignation(proto, data)` and `RemoveDesignation(origin)` are the practical APIs for temporary replacement.
- `IAreaManagingTower.ManagedDesignations` exposes designations managed by a tower. `MineTower` also has `ManagedDumpingDesignations`.
- `MineTower` has per-tower dumping state:
  - `DumpableProducts`
  - `AddProductToDump(LooseProductProto)`
  - `RemoveProductToDump(LooseProductProto)`
- `ITerrainDumpingManager` has standalone/global dumping state:
  - `AllDumpableProducts`
  - `ProductsAllowedToDump`
  - `AddProductToDump(LooseProductProto)`
  - `RemoveProductToDump(LooseProductProto)`
- Mafi code inspection showed tower-managed dumping is gated by `MineTower.DumpableProducts`, not `ITerrainDumpingManager.ProductsAllowedToDump`. `MineTower.CanAcceptDumpOf(product)` only checks whether the tower is enabled and its own dumpable product set contains the product.
- Dirt and compost exist both as terrain materials and products:
  - terrain material ids include `Dirt_Terrain`, dirt variants, `FarmGround_Terrain`, `Compost_Terrain`
  - product ids include `Product_Dirt` and `Product_Compost`
- Farming should not hard-code only dirt/compost names when the material proto is available. `LooseProductProto` has optional `TerrainMaterial`, and `TerrainMaterialProto` has `IsFarmable`, so dumped farmable products can be discovered programmatically.
- `FarmableManager.GetFarmableThickness(Tile2i tile, ThicknessTilesF maxValueAllowed, bool ignoreAutoSurfaces, out bool surfaceInWay)` is the closest game-side surface-level helper, but it does not obviously expose "check this exact z-band."
- `FarmFertileGroundValidator` uses these important thresholds:
  - minimum farmable thickness: `0.9` tiles
  - total missing farmable thickness budget: `0.9 * 5% * occupied vertex count`
- Save hooks exist, but ordering needs verification:
  - `SaveManager` has `OnSaveStart` and `OnSaveDone` fields.
  - `GameSaver.StartSave(...)` and `FinishSaveWriteToStream(...)` are likely serialization boundaries if patching is safer than event subscription.

## Decisions From Planning Review

- Save/load can re-analyze. This is preferred because it makes the feature tolerant of terrain changes, partial completion, and player edits.
- Pause/resume should behave the same way: pause restores canonical visible designations and tower dump rules; resume throws away transient phase state and runs a new analysis.
- Use `TerrainMaterialProto.IsFarmable` for target-band validation when possible.
- If `FarmableManager.GetFarmableThickness` cannot target a specific z-layer/band, use it as a corroborating validator and fall back to explicit layer enumeration with `IsFarmable`.
- During development, add a debug-only assert/log when an origin is considered done: the final target band must resolve to farmable material according to `IsFarmable`.
- Filling should allow all loose products whose dumped terrain material is farmable. In vanilla this should include dirt/compost-like products, but the implementation should discover them from `LooseProductProto.TerrainMaterial.Value.IsFarmable` for future game updates and mod compatibility. Only manipulate tower rules (`MineTower.DumpableProducts`). Do not inspect or change global dumping permissions for tower filling.
- "Hold" in the backlog maps to the same canonical state as pause/save: temporary designations removed, original level designations visible, and tower dump rules restored.

## Plan Gaps And Ambiguities

### 1. "Flat leveling designation" must be defined

Leveling designations can have four different corner heights. Farming mode should only capture level designations where all four `DesignationData` target heights match unless we intentionally add normalization for non-flat designations.

Non-flat handling is still a product/design decision. See "Questions For Non-Flat Designations" below.

### 2. "Soil materials" should mean farmable material

Using only dirt and compost risks missing farmable dirt variants, farm ground, weathered/recovered variants, future materials, and modded products. Use `TerrainMaterialProto.IsFarmable` when checking layer contents.

For filling products, build a dynamic set from loose products where:

- the product can be dumped/placed on terrain
- `LooseProductProto.TerrainMaterial` has a value
- that terrain material has `IsFarmable == true`

Then apply that dynamic set to the tower's dump rules during Filling.

### 3. Target layer logic needs an exact band

The backlog says "the entire layer directly below the target height." Terrain layers are thickness-based, not necessarily one discrete material per integer height. A tile can have mixed top material, fractional heights, surfaces, and bedrock.

Define the desired farmable band as `[targetHeight - 1, targetHeight]`. For each 1x1 tile cell inside the 4x4 designation footprint, classify the material in that band. If `FarmableManager.GetFarmableThickness` cannot inspect that exact band, enumerate terrain layers and check `TerrainMaterialProto.IsFarmable` directly.

### 4. Save/load should intentionally re-analyze

Before save, temporary preparation designations should be removed and original leveling designations restored. The save file should contain normal game state, not temporary ATD phase state.

On load, "resume farming mode" means:

- detect eligible flat level designations
- rebuild transient sessions
- analyze current terrain from scratch

Do not promise to preserve:

- current phase per origin
- exact previous automation ownership of every designation
- partially applied tower dump changes

If a cheap tower-level farming-mode flag can be persisted, use it only to decide which towers auto-reanalyze. Do not persist per-origin phase state unless later testing proves it is needed.

### 5. Dumping restrictions are tower-only

Mafi has both standalone/global `ITerrainDumpingManager.ProductsAllowedToDump` and per-tower `MineTower.DumpableProducts`. For tower-managed dumping, the tower's own `DumpableProducts` are the relevant gate. Farming mode must not read global dumping as a blocker for tower Filling.

For filling:

- snapshot the selected tower's `DumpableProducts`
- restrict that tower to the discovered farmable dump product set
- if no farmable dump product exists in prototypes, block/warn
- restore tower rules on done, cancel, pause, and before save

### 6. Original dumping restrictions can become stale

If farming mode snapshots the tower's dumpable products, then the player changes them while filling is active, blindly restoring the snapshot can erase the player's change.

Preferred policy: while automation owns dump rules, pause/resume is the supported edit path. Pausing restores normal tower rules, lets the player edit, and resuming re-analyzes with a fresh snapshot.

Minimum implementation: restore the saved snapshot on done/cancel/pause/save, and document that dump rule edits during active filling may be overwritten.

### 7. Overlapping tower areas can fight

Two mine towers with overlapping areas could see and mutate the same level designation. The session owner should be the tower where farming mode is active. Ignore designations already owned by another active farming session.

### 8. Player edits during automation need rules

Suggested behavior:

- if original/filling designation is removed by the player, drop that origin from the session
- if temporary prep designation is removed by the player, consider that origin cancelled
- if target height changes, restart analysis for that origin
- if the tower farming toggle is turned off, restore originals and dump rules immediately
- if the player pauses farming automation, restore canonical state and wait for resume

Pause/resume is important because it gives the player a clean edit window. On resume, discard per-origin transient state and run a fresh analysis.

### 9. Progress checks should use designation fulfillment state

Preparation completion should use the game's fulfillment flags where possible (`IsMiningFulfilled`, `IsDumpingFulfilled`, `IsFulfilled`). For preparation, the success condition is not "farmable at target" yet; it is "the target band is clear enough and/or the terrain is at least one z below target so filling can create the topsoil layer." Still re-run material checks before moving to filling and before marking done because a fulfilled designation can be fulfilled with the wrong material if tower dump rules did not point at the expected farmable dump products.

Analysis/Leveling also needs progress checks: if a designation is fully farmable but above target, leave the original level designation active until the game has leveled it to target, then re-run farmability analysis before deciding whether it is done or should move to filling/preparation.

### 10. Fertility is not the only farm placement rule

The checked validator is the fertile ground validator. Actual farm placement can also fail due to area, entity occupancy, tree props, surfaces, water/ocean, logistics, or building blockers. Farming prep should promise "terrain prepared for fertile ground," not "farm placement will always succeed."

### 11. TODO: mirror Mafi's farmable deficit budget

The current ATD analyzer is stricter than farm placement. It treats any non-farmable material in the future top band as `NeedsPreparation`.

Mafi's `FarmFertileGroundValidator` instead computes missing farmable thickness per occupied tile:

- required thickness per tile is `0.9`
- missing thickness is `0.9 - FarmableManager.GetFarmableThickness(tile, 0.9, ...)`
- missing thickness is accumulated across the footprint
- placement fails only when that total exceeds `0.9 * 5% * occupied vertex count`, or when a blocking surface is in the way

Later, consider replacing ATD's strict per-cell non-farmable test with a farm-placement-compatible deficit budget. This may avoid unnecessary preparation when a small portion of the farm footprint is imperfect but still accepted by Mafi.

## Recommended Implementation Shape

### New state

Add a new partial file, likely `ATD.FarmingDesignations.cs`, with:

- `FarmingSession` keyed by tower `EntityId`
- `FarmingOriginState` keyed by designation origin
- phase enum: `AnalyzingLeveling`, `Preparing`, `Filling`, `Done`, `Blocked`, `Paused`
- original `DesignationData`
- temporary preparation `DesignationData`
- target height
- last known original proto id
- snapshot of tower dumpable products

Do not keep player-cancelled origins as owned session state. If the player removes or replaces a tracked designation, drop that origin from the session so a newly placed flat level designation at the same origin can be captured fresh on the next analysis pass.

Keep the existing mining scan settings separate. Farming mode is a different workflow and should not reuse ore settings.

### Dependencies to resolve

In mod initialization, resolve and store:

- `ITerrainDesignationsManager` or concrete `TerrainDesignationsManager`
- `FarmableManager`
- `ProtosDb`
- optionally `SaveManager` or patch `GameSaver`

Build and refresh a farmable dump product set from `ProtosDb.All<LooseProductProto>()`, `LooseProductProto.TerrainMaterial`, and `TerrainMaterialProto.IsFarmable`. Dirt/compost ids can remain useful for debug labels or sanity checks, but should not be the primary selection mechanism.

### UI

Add a farming mode toggle to the mine tower inspector panel, near the existing terrain designations controls. Include a pause/resume control.

Status should be compact:

- off
- paused
- analyzing/leveling
- preparing `n/m`
- filling `n/m`
- blocked with reason

The UI should make it clear that active farming mode owns flat level designations in that tower area until paused, cancelled, or completed.

### Designation capture

When farming mode is enabled or resumed:

1. Enumerate `tower.ManagedDesignations`.
2. Keep only `designation.Prototype == s_levelingProto`.
3. Keep only flat data where all four target heights are equal.
4. Skip origins already owned by another active farming session.
5. Store original `DesignationData`.
6. Analyze each origin.

For new player placements while farming mode is active, subscribe to designation events or patch `TerrainDesignationsManager.AddOrReplaceDesignation`. The existing corner mode already patches `AddOrReplaceDesignation`, so farming interception must coexist cleanly with that prefix.

### Analysis/Leveling

For each origin:

1. Define `targetHeight` from the flat original designation.
2. Define farmable band as `[targetHeight - 1, targetHeight]`.
3. For each tile cell inside the 4x4 designation footprint, prefer layer enumeration plus `TerrainMaterialProto.IsFarmable` for the target band.
4. Use `FarmableManager.GetFarmableThickness` as a corroborating final validator if it cannot be aimed at a specific z-band.
5. Classify the designation:
   - fully farmable and fully at target: store the original for Hold/Done and mark done.
   - fully farmable and every cell is at or above target: leave the original level designation active so the game can level it, then re-analyze when it changes/fulfills.
   - fully farmable but at least one cell is below target: store the original for Hold/Filling; this origin only needs topsoil filling.
   - somewhere non-farmable: proceed to preparation.
6. Stay in Analysis/Leveling while any farmable-but-high designation is still being leveled.
7. Proceed to Preparation only after all remaining origins are either done, waiting for filling, or known non-farmable.

### Preparation phase

For prep origins:

1. Assert the origin is not fully farmable.
2. Store any remaining active original designation for Hold/Filling.
3. Remove the original leveling designation.
4. Add a flat `LevelDesignator` at `targetHeight - 1`.
5. Wait until the prep designation reports completely excavated/fulfilled.
6. Re-check that the terrain is ready for filling: either fully farmable already, or at least one z below target with no non-farmable material occupying the future topsoil band. If not, stay preparing or mark blocked with a reason.

### Filling phase

For fill origins:

1. Snapshot the tower's dumpable product set.
2. Ensure the discovered farmable dump products are allowed on the tower. Do not inspect or change global dumping permissions for tower filling.
3. Remove temporary prep designation.
4. Restore original flat leveling designation at `targetHeight`.
5. Wait for fulfillment and farmable-band check.
6. Restore tower dump rules when all session origins are done, cancelled, paused, or before save.
7. In debug builds, assert/log if a done origin has non-farmable material in the target band.

### Pause/resume behavior

When the player pauses farming automation for a tower:

1. Restore original/canonical level designations for all active origins.
2. Remove temporary prep designations.
3. Restore tower dump rules.
4. Mark the tower session paused.

When the player resumes:

1. Clear per-origin transient state.
2. Enumerate the tower's current flat level designations.
3. Run fresh analysis.

This provides a manual edit window and exercises the same robust path used after load.

The backlog's "Hold" state should be implemented as this same pause/canonicalization behavior, even if the UI label ends up being "Hold" rather than "Pause."

### Save behavior

Preferred safe path:

1. Before game serialization, call `RestoreCanonicalForSave()`:
   - remove all temporary prep designations
   - restore original leveling designations
   - restore tower dump rules
2. Let the game save.
3. After the save snapshot is complete, call `ReapplyTransientFarmingState()` if the current in-game session should continue.

Implementation option:

- Patch `GameSaver.StartSave(...)` with a prefix/postfix if `StartSave` creates the serialized memory snapshot synchronously.
- Alternatively, subscribe to `SaveManager.OnSaveStart` and `OnSaveDone`, but verify ordering before trusting it.

The save/load invariant should be: save files contain normal level designations and normal tower dump rules, not temporary prep/fill state.

## Suggested Milestones

1. Add read-only farming analysis command or debug/status bar that reports eligible level designations, target height, relative terrain height, and farmable status per origin.
2. Add tower UI toggle and in-memory session state, with no mutation yet.
3. Implement capture/cancel/restore of original level designations.
4. Implement Analysis/Leveling classification, including the "farmable and above target stays active" path.
5. Implement preparation phase at `targetHeight - 1`.
6. Implement filling phase with per-tower dynamic farmable-product dumping control.
7. Add pause/hold/resume with canonical restore and fresh re-analysis.
8. Add save boundary restore/reapply.
9. Add load/unpause behavior using conservative re-analysis.
10. Add player-edit cancellation rules and overlap protection.

## Open Questions Before Coding

- Should farming mode affect only new level designations after the toggle, or also all existing flat level designations in the tower area?
- What should happen if a tile already has enough farmable thickness but is at the wrong height because the original designation is partially fulfilled?
- Should the UI call the canonical stop state "Hold," "Pause," or use both terms?

### Questions For Non-Flat Designations

- Should non-flat level designations be ignored silently, or should the UI show "skipped non-flat designations"?
- If a non-flat designation has only one corner off by one because of terrain smoothing/corner mode, should farming mode reject it, or normalize it to the most common target height?
- If normalization is allowed, should it require player confirmation because it changes the intended terrain shape?
- Should farming mode ever process ramps/gradients for terrace farming, or is the feature strictly flat-farm preparation?

## Bottom Line

The feature is feasible with the existing designation manager APIs. The implementation should lean into re-analysis after load/resume, use `IsFarmable` for layer validation and dump-product discovery, manipulate only tower dump rules, and provide pause/resume so player edits become a first-class workflow instead of an edge case.
