# Backlog

## Ramp safety margin - low priority
* Review ramp building safety margin logic in more depth. Current margin is based on ramp planning depth rather than actual vertical drop relative to surrounding surface, so it may not fully reflect landslide/building risk in uneven terrain.

## Farming Designations
Goal: When player designate land for farming, it is automatically prepared for farming.

Farming preconditions: Flat, fertile land.

Suggested approach: 
From tower, a button to flip between normal mode and farming mode. When the tower is in farming mode, leveling designations (pre-existing or new) behave in special way.

When entering farming mode or user places a leveling designation: Store the leveling designation(s) internally (will be referenced and restored later), analyze the land where the designation is placed and prepare it for farming. Preparation for farming will need to happen in several steps, du to how dumping rules on the tower works:
* Define the target height as the level of the original leveling designation. Store the leveling designation internally as the original farming designation (to be restored later).
Analysis/Leveling:
* If the designation is already fully farmable and fully at target level: store to be restored at Hold or Done.
* Else If the designation is fully farmable and everywhere at or above target level, leave the designation in place for it to be leveled (will be analyzed again for farmability again when leveled).
* Else If the tile is fully farmable: store designation to be restored at Hold or Filling.
* Assert: Tile is somewhere not farmable => proceed to Preparation.
* When all designations are done/leveled, proceed to Preparation. Else stay in Analysis/Leveling.
Preparation [assert: all remaining designations are not fully fertile]:
* Store remaining active designations internally (to be restored at Hold or Filling). Place leveling designations one level below the designations just stored. This will excavate the infertile designations to make room for the topsoil.
* Keep Preparation until all designations are completely excavated.
Filling [assert: all designations are farmable or at least 1 z below target level]:
* Restrict dumping in the tower to just dirt/compost. (Store original tower dumping restrictions; will be restored later)
* Restore all designations previously stored.
* Keep Filling until all tiles are completely filled.
* Restore original dumping restrictions on tower.


Important events:
* Save game (or pause) must be patched to remove all temporary designations and restore the original leveling designations before the game is saved. Also, restore original dumping restrictions.
* Initialization/Game load (unpause): resume farming mode on all towers with at least one leveling designation.

During implementation: use a debug/status bar.

## todo:
- tower avoidance
- laggy on big designations, even on 2x speed