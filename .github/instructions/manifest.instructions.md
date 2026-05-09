---
description: Instructions for maintaining the AutoTerrainDesignations manifest.json file.
applyTo: "manifest.json"
---

# manifest.json Maintenance Rules

## Reference
Official field definitions: https://github.com/MaFi-Games/Captain-of-industry-modding

---

## Required fields

| Field | Type | Rule |
|---|---|---|
| `id` | string | Must match `[a-zA-Z0-9][a-zA-Z0-9_-]*`. Must not start with `COI-`. Must match the mod folder name. |
| `version` | string | See **Versioning** below. |
| `primary_dlls` | string[] | List DLLs in load order. All must be in the mod directory. For ATD: `["0Harmony.dll", "AutoTerrainDesignations.dll"]`. |

## Optional fields in use

| Field | Rule |
|---|---|
| `display_name` | Max 50 characters. Human-readable name shown in the mod browser. |
| `description_short` | Max 180 characters. Plain text. Shown in mod list. |
| `description_long` | Detailed description. Supports basic HTML (`<b>`, `<i>`, `<br>`). |
| `authors` | String or array of strings. |
| `min_game_version` | Set to the oldest game version the mod is known to work with. |
| `max_verified_game_version` | Update to the current game version after each test session. |
| `mod_dependencies` | Required mod IDs. Supports `>=` version constraints (e.g. `"OtherMod >= 1.0.0"`). Use `[]` if none. |
| `optional_mod_dependencies` | Same syntax; loaded first if present but no error if absent. |
| `incompatible_mods` | Mod IDs that must not be loaded alongside this mod. |
| `non_locking_dll_load` | Keep `true` — allows hot-reloading DLLs without restarting the game. |
| `can_add_to_saved_game` | `true` if the mod can be safely added to an existing save. |
| `can_remove_from_saved_game` | `true` if the mod can be safely removed from an existing save. |
| `primary_mod_class_name` | Required when multiple `IMod` classes exist in the loaded DLLs. |

---

## Versioning

- Use `MAJOR.MINOR.PATCH` for public releases (e.g. `0.2.5`).
- Use `MAJOR.MINOR.PATCHletter` for local/alpha packages (e.g. `0.2.5a`, `0.2.5b`).
  - The game normalises `1.2.3a` → assembly version `1.2.3.1` (a=1, b=2, …).
- **Local/alpha builds**: increment the letter on each package. Update both `manifest.json` and `changelog.txt` to the new letter version.
- **Public releases**: collate all lettered alphas into a new patch or higher version number. Update both `manifest.json` and `changelog.txt`.
- The `version` in `manifest.json` must always match the top entry in `changelog.txt`.
- Do not propose version changes unless the user asks to package or release.

---

## Game version fields

- `min_game_version`: only lower this if you've verified compatibility with an older version.
- `max_verified_game_version`: update to the current game version whenever a new game update has been tested successfully with this mod.

---

## Constraints

- `id` must exactly match the directory name the mod is deployed in.
- `display_name` ≤ 50 characters.
- `description_short` ≤ 180 characters.
- Empty arrays (`[]`) are valid and preferred over omitting optional array fields entirely.
