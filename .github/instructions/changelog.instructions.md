---
description: Instructions for maintaining the AutoTerrainDesignations change log.
applyTo: "changelog.txt"
---

# Change Log Maintenance Rules

The changelog.txt in the root is a user-facing change log that complies with the Mafi requirements for the CoI mod portal, but is maintained in markdown format for ease of editing and pasting into other contexts like Discord announcements. When making changes to the mod that affect user-facing behavior, features, or fixes, the change log must be updated with a clear description of the change, following the formatting and content rules below.

## File location
`changelog.txt` in the workspace root (plain text, required by the Mafi mod portal).

## Entry suffix states
Each version header carries a suffix indicating its current state:

| Suffix | Meaning |
|---|---|
| `[unreleased]` | Work in progress — changes are being added to this entry |
| `[packaged]` | ZIP has been built and is ready to upload; not yet on the portal |
| *(no suffix)* | Released on the CoI mod portal |

## Format rules
- Each release starts with `v<semver> | <YYYY-MM-DD>` followed by the appropriate suffix (e.g. `v0.2.6 | 2026-05-08 [unreleased]`)
- Top-level bullet entries use `*`.
- Sub-bullets use 4 spaces followed by `-`.
- New changes are **added to the current top entry** (the one matching `manifest.json`). Do not create a new version entry for code changes alone.
- Do not bump the version or create a new entry unless explicitly asked to package.

## Content rules
- One bullet per user-visible change (feature, fix, or behavioral change).
- Internal refactors, code cleanup, and build tooling changes are omitted unless they affect behavior.
- API additions visible to external modders are documented (e.g. new `AutoTerrainDesignationsApi` methods).
- Settings file additions are documented with the key name(s) and what they control.
- Start each bullet with a capital letter; no trailing period.
- Use neutral, factual language. No marketing or selling tone (avoid words like "polished", "first-class", "powerful", etc.).
- Use **bold** for setting names and UI control names (e.g. **Ore Purity Level**, **Ramp Width**).
- Fixes start with `Fixed:` followed by a short description.
- Sub-bullets are used when a single feature has multiple related details worth calling out.

## Versioning
- Version numbers follow `MAJOR.MINOR.PATCH` (currently in `0.x.y` range).
- **Local/alpha builds**: Use a letter suffix — `0.2.5a`, `0.2.5b`, etc. Each local package gets the next letter. Both `changelog.txt` and `manifest.json` are updated to match the new letter version.
- **Public releases**: Collate all lettered alpha entries into a single new version (`0.2.5a + 0.2.5b → 0.2.6`). Increment PATCH for fixes/minor additions, MINOR for noticeable new features. Both `changelog.txt` and `manifest.json` are updated.
- The version in `changelog.txt` must always match `manifest.json`.
- **Do not bump the version or advance the entry until the user confirms the package was successfully released.**

## Packaging procedure (exact sequence)
When the user asks to build a package, follow these steps **in order** — do not skip ahead or combine steps:

1. **Commit and push** any uncommitted changes first.
2. **Replace `[unreleased]`** with `[packaged]` on the current top entry in `changelog.txt`. Do not change the version or date.
3. **Run the package build** (`build.ps1 -Configuration Release -Package`). The ZIP produced will carry the version in `manifest.json` at build time.
4. **Commit and push** the changelog change with a message like `chore: package <version>`.

**Never bump the version before running the build.** The ZIP file name comes from `manifest.json` at build time.

The user will upload the ZIP to the portal manually. Do not advance the version automatically.

## When starting new work after a package
When the user starts making changes and the current top entry is marked `[packaged]`, **ask whether it was released to the portal**:

- **Yes — released**: Replace `[packaged]` with no suffix (mark released), then add a new empty top entry `v<next> | <date> [unreleased]` in `changelog.txt` and update `manifest.json` to the next version. Commit and push with a message like `chore: release <version>, advance to <next> [unreleased]`.
- **No — not yet released** (e.g. the package had a problem): Keep the current entry and continue adding changes to it; replace `[packaged]` back with `[unreleased]` since it will need to be rebuilt.

## Example entries
```
v0.2.6 | 2026-05-10 [unreleased]   ← in progress
* Corner designations now snap height and variant to adjacent existing designations

v0.2.5b | 2026-05-08 [packaged]    ← ZIP built, not yet uploaded
* Fixed: ramp generation could place ramps outside the tower area

v0.2.5a | 2026-05-01               ← released on portal
* Added ore purity filter

v0.2.4 | 2026-04-20                ← released on portal
* ...
```
