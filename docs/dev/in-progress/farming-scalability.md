# Farming Automation Scalability

## Context

Large flat farming designation areas can be auto-reactivated on load when `reEnableFarmingOnLoad` is enabled. A tester save confirmed two active sessions with thousands of tracked origins:

- Tower 1: 2560 active origins, 1416 unreachable excavation origins.
- Tower 8: 1556 active origins, 910 unreachable excavation origins.

The current farming automation loop can scan all tracked origins and run expensive access/pathability work on the main Unity side. This can produce periodic stutter or freezes, especially when large sessions repeatedly request or re-check access ramps.

## Plan

1. Add performance logging around the largest farming passes.
   - Log slow preparation/filling/access/fill-area operations.
   - Include tower id, active origin count, elapsed milliseconds, and useful phase/access counts.
   - Cool down logs per session so large saves do not flood the game log.

2. Throttle access/pathability checks for large sessions.
   - Keep small sessions responsive.
   - Increase cache/recheck intervals as current work size grows.
   - Avoid rerunning the expensive BFS/ramp request loop every few ticks on thousand-origin sessions whose designation state has not materially changed.

3. Cache pending filling areas.
   - Store the fill-area `HashSet<Tile2i>` on the farming session.
   - Rebuild it only when queued filling origins or shoulder origins change.
   - Reuse it for vehicle clear-out and pruning.

4. Later: batch origin analysis.
   - Process only N origins per tick in preparation/filling passes.
   - Track cursors and dirty/full-scan state so phase transitions remain correct.
   - Do this after the lower-risk access throttling and fill-area cache are verified.

## Latest Tester Findings

After the access throttling, fill-area caching, and finer performance markers were added, the same tester save looked substantially smoother:

- Earlier log before the hotfix showed preparation pass spikes of roughly 3.5-31 seconds on tower `77674`.
- Latest alpha log (`26-05-16_17-06-13_6879.log`) showed no multi-second ATD preparation passes in the sampled run.
- The largest logged ATD farming rows were approximately 347-451 ms on the first load-time passes.
- Breakdown rows showed those costs were mostly access/ramp handling:
  - Tower `77674`: `capture=2ms`, `advance=85ms`, `access=256ms`, total `347ms`.
  - Tower `79671`: `capture=0ms`, `advance=52ms`, `access=398ms`, total `451ms`.
  - Later steady-state rows were generally around 30-158 ms, again mostly access time.
- Origin analysis was not the active bottleneck in this run. Large-session `advance` timing was typically 5-35 ms after the initial capture.
- The log still showed `Outer enumerator finished first?` asserts around access ramp placement. One stack involved `BuildBuildingOccupiedTiles -> CreateAccessRamp -> EnsureFarmingAccessForCurrentPhase`, while CLExporter was also reflectively enumerating entities from its timer. This suggests remaining intermittent hitch/assert risk may come from broad entity enumeration during ramp placement, especially when another mod is scanning entities concurrently.

**Second tester run** (`26-05-17_08-34-17_1893.log`) confirmed the CLExporter-contention spike:

- Steady-state preparation passes for tower `77674` (1533 origins, ~916 analyzed, ~617 hidden) ran at 30–35 ms (`access` ≈ 25–31 ms).
- Three back-to-back passes spiked to **2859 ms → 3952 ms → 18009 ms**, all from `access` alone (`advance` was stable at 5 ms throughout).
- After the 18 s spike, the `work` and `inaccessible` counts dropped (894/845 vs 910-915/869-873), confirming actual ramp placement had occurred during the spike.
- The `access check` sub-log (BFS pathfinding) showed only 25–45 ms each time, ruling out BFS as the cause. The extra time was entirely inside `CreateAccessRamp` → `BuildBuildingOccupiedTiles` → `GetAllEntitiesOfType<IStaticEntity>()`, contending with CLExporter's concurrent entity scan.

**Third tester run** (`26-05-17_10-07-12_7724.log`) after the ramp-search optimizations (perimeter candidate filter, UpdateChangedTiles hoist, BFS dedup, cap, reduced constants):

- **No spikes above 400 ms.** Previous runs had 15–22 s spikes; worst case now is ~378 ms.
- Most preparation passes for tower `77674` (1533 origins) ran at **25–107 ms**.
- Occasional elevated passes (100–378 ms) were all `access`-dominated, consistent with `CreateAccessRamp` → `BuildDesignationOriginsInArea` → `SelectDesignationsInArea` still called on every `CreateAccessRamp` invocation without a TTL cache. This is the remaining known bottleneck.
- The two "No usable 1-wide ramp space found" entries confirm tower `77674` is in a configuration where no valid corridor exists, so `CreateAccessRamp` runs to exhaustion on every access tick that reaches ramp placement.

## Current Implementation Notes

- Added slow-operation logging for preparation passes, filling passes, access checks, and pending fill-area rebuilds. Logs use the prefix `[ATD Farming Perf]` and are thresholded/cooldown-limited.
- Added a slow preparation breakdown row with capture/advance/access/summary/state-scan timings plus origin counts, so multi-second preparation spikes can be attributed without enabling broader debug logging.
- Added `tools/get-mod-log.ps1` to extract `[ATD`/`[AFD` rows from the newest Captain of Industry log, or from an explicitly supplied log path. (Supersedes the older `tools/extract-atd-farming-perf.ps1`.)
- Access checks now keep the existing 10-tick recheck for small work sets, 30 ticks for 250+ active work designations, and 90 ticks for 1000+ active work designations.
- Pending filling areas are cached per session and rebuilt only when queued filling/rim/shoulder/origin state changes.
- `BuildBuildingOccupiedTiles` caches its result per tower ID for up to 600 farming ticks (~60 s). The cache is invalidated automatically when the tower changes or the farming tick counter is reset. This eliminates the primary CLExporter-contention window.
- `IsFreeRampTile` now uses a per-call `s_designationOriginsInArea` `HashSet<Tile2i>` populated by a single `SelectDesignationsInArea` call in `BuildDesignationOriginsInArea`, instead of calling `GetDesignationAt` per tile (~66 K calls previously). `PlaceDesignation` keeps the set consistent so freshly placed ramp origins are always reflected.
- `CollectRampCandidates` now builds a `perimeterOreTiles` list and iterates only tiles with at least one non-ore cardinal neighbour. Interior tiles (all four neighbours ore) cannot produce a valid ramp exit within the attachment-depth limit and are skipped. For the tester save (791 ore tiles), this reduces iterations from ~791 to ~108 tiles (~7×).
- `UpdateChangedTiles()` is called once per `TryPlaceRampCandidates` invocation (hoisted out of the per-candidate loop). Ramp-mouth reachability BFS results are cached by position within the same call (`testedMouthReachability` dict). Checks are capped at `MAX_RAMP_REACHABILITY_CHECKS = 50`; candidates beyond the cap fall back to the best already-checked position.
- `RAMP_ACCESS_SEARCH_MARGIN_TILES` reduced from 96 to 48 tiles; `MAX_RAMP_ACCESS_SEARCH_TILES` reduced from 250 000 to 20 000.
- Full origin analysis batching remains the next larger refactor.

## Recommended Next Step

Add a TTL cache to `BuildDesignationOriginsInArea` using the same pattern as `BuildBuildingOccupiedTiles` (per-tower ID + farming tick counter). `SelectDesignationsInArea` currently performs 5 625 `m_designations` dictionary lookups for a ~300×300 tower area; under navigation lock contention each lookup can block ~2.7 ms, making uncached calls to `BuildDesignationOriginsInArea` the remaining primary source of elevated `access` times. A 30-tick TTL would limit the call to at most once every ~3 s per tower. `PlaceDesignation` already updates `s_designationOriginsInArea` on every write, so the cache is always current for freshly placed designations regardless of TTL.

## Non-goals For This Pass

- Do not disable `reEnableFarmingOnLoad` by default. Save persistence should replace that workaround later.
- Do not change user-facing farming behavior beyond reducing repeated expensive work.
- Do not change designation semantics or tower dump-rule timing unless profiling proves it is needed.
