# Farming Designations Staged Rollout

## Principle

Do not implement the whole feature at once. Build a narrow, observable slice first, validate it in-game, then widen the automation. The goal is to catch incorrect terrain/material assumptions before tower-level dump-rule ownership enters the picture.

## Stage 1: Read-Only Analyzer

Add a debug/status command or panel output for the selected tower.

It should:

- find flat level designations
- compute each designation's target height
- classify each origin as `Done`, `NeedsLeveling`, `ReadyForFilling`, `NeedsPreparation`, `Blocked`, or `SkippedNonFlat`
- list discovered farmable dump products from `LooseProductProto.TerrainMaterial.IsFarmable`

No terrain/designation/dump-rule mutation in this stage.

## Stage 2: Single-Origin Debug Transition

Add a debug command that targets one designation origin and performs only the next safe transition.

Examples:

- `NeedsPreparation`: store the original and replace it with a flat `LevelDesignator` at `targetHeight - 1`.
- `Restore/Cancel`: restore the stored original designation for this one origin.

This validates original storage/restoration, `AddOrReplaceDesignation`, and target-height math without full tower automation.

This does not implement the full pause/hold/save model. It only proves the primitive needed later: "given one temporary preparation designation, can we safely restore its original?"

Implemented debug commands:

- `atd_farming_analyze_origin x y`: snap `x y` to the 4x4 designation origin and print the Stage 1 classification.
- `atd_farming_prepare_origin x y`: only if the origin is `NeedsPreparation`, store the original flat level designation and replace it with a temporary flat level designation at `targetHeight - 1`.
- `atd_farming_restore_origin x y`: restore the original designation stored by the prepare command.

## Stage 3: Per-Origin Analysis And Preparation Loop

Let multiple origins independently progress through:

- `Analysis/Leveling`
- `Preparing`
- `ReadyForFilling`
- `Done`
- `Blocked`

Still do not manipulate tower dump rules. Filling remains disabled until preparation behavior is proven.

Implemented as tower-scoped automation from the Farming Prep Analysis panel:

- `Farming automation` toggle on: continuously scan the selected tower's flat level designations, prepare infertile/rock target bands, enter filling only when the whole tracked tower set is ready, and keep monitoring after the inspector is closed.
- `Farming automation` toggle off: disable the tower session, restore stored original level designations, and restore tower dump rules if ATD owns them.

The session repeatedly re-analyzes visible level designations for that tower. It stores each flat original designation, leaves `NeedsLeveling` origins alone, replaces `NeedsPreparation` origins with temporary flat `LevelDesignator` designations at `targetHeight - 1`, and stops once all tracked origins are `ReadyForFilling`, `Done`, or `Blocked`.

If a tracked designation is removed or replaced by the player, Stage 3 drops that origin from the session instead of storing `Cancelled`. This lets a player put the designation back and have it captured fresh on the next pass.

Stage 3 originally stopped before filling. During the uninterrupted full-cycle test, any session that reaches `ReadyForFilling` is allowed to continue into Stage 4 automatically on the background ticker.

## Stage 4: Tower-Level Filling Window

Once per-origin prep is stable, add tower-level filling.

When the tower enters Filling:

- snapshot tower dump rules
- restrict the tower to the discovered farmable dump products
- restore all fill-ready original designations
- wait for fulfillment and final farmable-band validation
- restore tower dump rules

This stage is tower-level because topsoil filling needs distinct tower dump rules.

Implemented as a Stage 4 action from the Farming Prep Analysis panel:

- plus icon: start filling for the selected tower's existing `ReadyForFilling` origins

The filling action requires an existing Stage 3 session. It does not run preparation itself. When started, it snapshots the selected tower's current dump rules, restricts that tower to discovered farmable dump products, restores the original level designations for all `ReadyForFilling` origins, and monitors them until they validate as `Done` or become `Blocked`.

If a tracked filling designation is removed or replaced by the player, Stage 4 drops that origin from the session instead of storing `Blocked`. This matches Stage 3 edit behavior: player edits release ATD ownership so a newly placed flat level designation can be captured fresh later.

When no origins remain in `Filling`, the tower dump rules are restored from the snapshot. Global dump rules are never inspected or changed.

## Stage 4.5: Uninterrupted Full-Cycle Test

Before implementing pause/hold/save/load behavior, prove the uninterrupted happy path:

- rough infertile terrain starts as flat level designations
- per-origin preparation runs until each origin is `ReadyForFilling`, `Done`, or `Blocked`
- the tower automatically enters filling only when every tracked current flat level designation in the tower area is `ReadyForFilling` or already `Done`
- tower dump rules are restricted only during filling
- filling is monitored until origins validate as `Done` or become `Blocked`
- the status window refreshes while the player manages trucks and excavators

Implemented as the `Farming automation` toggle in the Farming Prep Analysis panel. It starts a continuous full-cycle session for the selected tower. The session keeps scanning while enabled, so newly placed flat level designations in the tower area are captured on later ticks.

Farming sessions are advanced by the always-on `AutoTerrainDesignationsTicker`, not by the inspector UI. Closing the mine tower inspector does not stop active preparation/filling monitoring. The inspector is only a control and status surface.

The existing controls are:

- refresh icon: read-only analysis
- `Farming automation` toggle: enable/disable all farming preparation/filling behavior for this tower

If a session is already inactive but has `ReadyForFilling` origins from an earlier preparation-only run, the background ticker runs one more preparation pass to capture current flat level designations in the tower area. It enters filling only if all tracked origins are `ReadyForFilling` or `Done`; otherwise it resumes preparation/leveling monitoring.

## Stage 4.6: Access Ramp Checks

Farming automation also needs vehicle access to the work. A farm tile can be correctly classified and still be impossible for excavators or trucks to reach if surrounding terrain is too steep or isolated.

First implementation slice:

- reuse the existing vehicle pathability flood-fill used by ATD ramp validation
- check tracked farming designations every background tick
- if preparation/leveling work is unreachable, request an excavation access ramp using the existing ramp planner
- if filling work is unreachable, request a dumping access ramp using the same planner with the `DumpingDesignator`
- hold the transition into tower-level filling while a preparation access ramp is still needed
- keep the access-ramp result visible in the farming status text

This is intentionally still narrow. It checks access per tracked 4x4 farming designation origin, not a full per-cell Mafi job-goal simulation. If it proves insufficient in testing, the next refinement is to mirror `TerrainDesignationVehicleGoal` target-tile expansion more closely for every unfulfilled cell.

## Stage 5: Pause/Hold/Save Canonicalization

Add canonicalization before save/load polish. This is where the full pause/hold behavior belongs, after Stage 4 has introduced tower-level filling and dump-rule ownership.

Pause/Hold/Save should:

- remove temporary prep designations
- restore original level designations
- restore tower dump rules

Resume/Load should:

- discard transient per-origin state
- re-analyze visible level designations from current terrain

## Stage 6: Player Edit And Overlap Hardening

Handle the messy edges after the core loop works:

- player removes or changes original/fill designations
- player removes temporary prep designations
- target height changes
- tower area overlap
- no farmable dump products discovered
- non-flat designation policy
- UI wording for Hold vs Pause

## First Implementation Target

Start with Stage 1 plus the smallest possible Stage 2 command. The first real mutation should affect exactly one origin, and it should always be reversible by restoring the stored original designation.
