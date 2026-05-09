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
Analysis:
* If the tile is empty or has only soil materials (=dirt or compost) in the target layer (=the entire layer directly below the target height in the farming designation), delete the farming designation temporarily (will be restored in a later phase)
Assert: the target layer has at least some material in it that is not soil.
Preparation:
* Place a leveling designation one level below the target level. This will prepare the land for the topsoil, by excavating any non-fertile materials and filling the tile to z-1 with any materials set by the tower rules.
* Keep Preparation until all tiles are prepared.
Filling:
* Restrict dumping in the tower to just dirt. (Store original dumping restrictions; will be restored later)
* Restore the farming designations.
Keep Filling until all tiles are filled.
* When done, restore original dumping restricitons.

Important events:
* Save game must be patched to remove all temporary designations and restore the original leveling designations before the game is saved.
* Initialization/Game load: resume farming mode on all towers with at least one leveling designation.
