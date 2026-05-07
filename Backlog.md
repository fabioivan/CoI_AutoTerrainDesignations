# Feature: Corner designations

Vanilla allows placing manual mining designations (M-key)

They come in the following forms:
- flat (all corners on same z)
- slope (opposite edges differ by 1 z)

F-key switches between the two modes.

Vanilla allows rotating the slopes (R-key), to get 4 variants. The player can also change elevation of the designation in hand (E=up, Q=down), place with LMB, and remove with RMB.

ATD can place what I refer to "corner designations", characterized by one(1) corner having a different z 1 above ("outer corner") and 1 below ("inner corner"). With rotations, 8 variants in total.

However, player can't place these manually. This is what we want to enable.

What we need (for MVP):
- Toolbar buttons (1 for inner corner and one for outer, in the Terraforming menu, under a new sub-category "Designations")
- Clicking buttons should give the player a corner designation in hand, rotatable with R, placable with LMB.