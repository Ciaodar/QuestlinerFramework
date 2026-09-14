# Agent Knowledge Base: Questliner Framework

This document provides technical insights, architectural patterns, and development guidelines for AI agents working on **Questliner Framework**.

## Project Architecture

### 1. Mod Initialization (`QuestlinerSubModule.cs`)
The mod uses a standard `MBSubModuleBase` entry point:
- **`OnBeforeInitialModuleScreenSetAsRoot`**: Scans all active Bannerlord modules for `ModuleData/Questlines/*.xml` via `QuestModuleScanner`.
- **`OnGameStart`**: Registers `QuestlinerCampaignBehavior` when the game type is `Campaign`.

### 2. External Questline Loading (`QuestModuleScanner.cs`)
- Allows 3rd-party mods created by **Questliner.Creator** (or hand-crafted) to load seamlessly.
- Modules declaring `<DependedModule Id="QuestlinerFramework" />` will be loaded after Questliner.
- The scanner traverses all enabled modules, detects XML definitions under `ModuleData/Questlines/`, and deserializes them into `QuestModel` definitions in memory.

### 3. Core Engine (`DynamicQuest.cs`)
- Subclasses TaleWorlds' native `QuestBase`.
- Implements a state-machine driven by `QuestStage` nodes (Logs).
- Progresses stages when entry/completion conditions are met.

### 4. Automatic Version Management
Build-time version bumping via `Build/UpdateVersion.ps1`:
- **Version format**: `vMAJOR.UPDATE.HOTFIX.DEBUG` (e.g., `v1.0.0.0`).
- Increment rules match SMS and JGUM conventions.

## Development Guidelines
- Always verify `Hero.MainHero` and `Campaign.Current` for null-safety before interacting with game systems.
- Localization: All user-facing strings must support Bannerlord's `{=ID}Text` format.
