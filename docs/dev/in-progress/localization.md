# Localization — Current System

> **Status: in-progress — this system is being replaced.**
>
> The custom `AtdLocalization` class described here will be replaced by the
> localization framework from `CoI_AutoHelpers`. When migrating, preserve all
> existing translation key names and their `translations/*.json` values.
> See `external/CoI_AutoHelpers/src/CoI.AutoHelpers/Localization/` for the
> replacement API.

---

## Current implementation

`AtdLocalization` (`src/ATD.Localization.cs`) is an internal static class.

### Initialization

```csharp
AtdLocalization.Initialize(modRootPath);
```

Called once from `AutoTerrainDesignationsMod.Initialize`. Loads translation files from `<modRootPath>/translations/`.

### Public methods

```csharp
string Tr(string key, string englishDefault)
```
Returns the translation for `key`. Falls back to `englishDefault` if no matching entry exists in either the selected language or English. A one-time warning is logged on first fallback.

```csharp
string TrFormat(string key, string englishDefault, params object[] args)
```
Like `Tr` but runs `string.Format(template, args)` on the result.

```csharp
string Tt(string key, string englishDefault)
```
Like `Tr` but appends `"\n[ATD]"` — used for tooltip text so the player can see
which mod added the tooltip.

---

## Translation files

Stored in `translations/` at the mod root.

- One file per language: `en.json`, `de.json`, `zh.json`, etc.
- JSON object: stable string key → translated string.
- Keys use dot-separated namespacing, e.g. `panel.farming.automation_toggle.label`.

`en.json` is always loaded first as the fallback. If no language-specific file matches, English strings are used.

---

## Language resolution order

Given the current game language (from `LocalizationManager.CurrentLangInfo`),
the system builds a candidate list and tries each in order:

1. Full filename without extension from `LangInfo.FileName` (e.g. `zh-TW`)
2. Full normalized culture ID (e.g. `zh-tw`)
3. Two-letter language code (e.g. `zh`)
4. Swedish alias: `se` ↔ `sv` are treated as interchangeable
5. `en` (always last)

The first candidate with a non-empty translation file wins. Once a file is loaded its count is checked; an empty file is treated as missing.

---

## What to preserve when migrating to AutoHelpers

- All existing key strings (callers reference them by literal string; changing a
  key is a refactor, not just a config change).
- All `translations/*.json` files and their content.
- The `Tt` suffix convention (tooltip = translation + mod marker).
- The one-time fallback logging behavior.

The `TrFormat` positional-argument pattern may need to change if the AutoHelpers framework uses named tokens instead of `{0}` / `{1}` placeholders. Audit callers of `TrFormat` before migrating.
