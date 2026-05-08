---
description: Describe when these instructions should be loaded by the agent based on task context
# applyTo: 'Describe when these instructions should be loaded by the agent based on task context' # when provided, instructions will automatically be added to the request context when the pattern matches an attached file
---

<!-- Tip: Use /create-instructions in chat to generate content with agent assistance -->

# Versioning and release model
- **Local/alpha packages** (built via `build.ps1 -Package`): increment the letter suffix on each package — `0.2.5a`, `0.2.5b`, etc. Update both `manifest.json` and `changelog.txt` to the new letter version.
- **Public releases** (GitHub + CoI Hub): collate all lettered alpha changes into a single new version (e.g. `0.2.5a + 0.2.5b + 0.2.5c → 0.2.6`). Update both `manifest.json` and `changelog.txt`.
- `manifest.json` version and the top `changelog.txt` entry must always match.
- Do not propose version changes unless the user explicitly asks to package or release.

# Externalize constants to settings file
If new constants or parameters are added to the mod, suggest that these be externalized into the settings file with clear descriptions, so power users can easily customize behavior without needing to modify code.

# Update change log with new features and fixes
After having added new features or fixes, suggest that the mod's change log be updated with clear descriptions of what was added or fixed, so users can easily see what's new in each version. If there is no entry for a new version, document unreleased changes in a new section at the end of the file for the upcoming version.

# Review API implementation for breaking changes and new features
After each major update, review the mod API implementation and suggest to update instructions with any new features, changes, or fixes that may impact users or modders. This ensures that the mod's documentation remains accurate and helpful for the community.
