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

## Current Implementation Notes

- Added slow-operation logging for preparation passes, filling passes, access checks, and pending fill-area rebuilds. Logs use the prefix `[ATD Farming Perf]` and are thresholded/cooldown-limited.
- Added a slow preparation breakdown row with capture/advance/access/summary/state-scan timings plus origin counts, so multi-second preparation spikes can be attributed without enabling broader debug logging.
- Added `tools/extract-atd-farming-perf.ps1` to extract `[ATD Farming Perf]` rows from the newest Captain of Industry log, or from an explicitly supplied log path.
- Access checks now keep the existing 10-tick recheck for small work sets, 30 ticks for 250+ active work designations, and 90 ticks for 1000+ active work designations.
- Pending filling areas are cached per session and rebuilt only when queued filling/rim/shoulder/origin state changes.
- Full origin analysis batching remains the next larger refactor.

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

## Recommended Next Step

Pause here for a new tester alpha. If further hitches are reported, prioritize reducing/caching the broad entity scan in `BuildBuildingOccupiedTiles` during access ramp placement before implementing full origin-analysis batching. Origin batching is still a useful scalability safety net, but the latest markers do not show it as the main remaining cost.

## Non-goals For This Pass

- Do not disable `reEnableFarmingOnLoad` by default. Save persistence should replace that workaround later.
- Do not change user-facing farming behavior beyond reducing repeated expensive work.
- Do not change designation semantics or tower dump-rule timing unless profiling proves it is needed.
