# Agent Knowledge Base: Questliner Framework

This document provides technical insights, architectural patterns, and development guidelines for AI agents and future developers working on the **Questliner Framework** mod for Mount & Blade II: Bannerlord.

## Project Architecture

The project is divided into two main assemblies to cleanly separate data models from execution logic:

### 1. Questliner.Shared
Contains all XML deserialization structures and data models.
- **`QuestModel.cs`**: Defines `Questline`, `StageModel`, `DialogNode`, `ConditionModel`, `ActionModel`, and `TargetResolverModel`.
- *(Note: `TimeoutInHours` is used alongside `TimeoutInDays` due to Bannerlord's Native quest engine restrictions on integer days.)*

### 2. Questliner.Framework
Contains the core engine that parses models, manages active quests, and executes logic.
- **`DynamicQuest.cs`**: The heart of the framework. Inherits from Native `QuestBase`. Manages stage transitions, journal logs, and dynamic UI tracking.
- **`QuestlinerCampaignBehavior.cs`**: Intercepts Native campaign events (dialogues, daily ticks, menu initializations) and injects framework logic.
- **`TargetResolverService.cs`**: The dynamic target assignment engine. 
- **`ConditionEvaluator.cs` & `ActionExecutor.cs`**: Process XML-defined conditions and actions dynamically.

## Core Features & Patterns

### 1. Data-Driven Design (Hardcoding Forbidden)
This framework is strictly **data-driven**. AI agents must **NEVER** write hardcoded C# logic tailored to a specific quest. If a quest requires a new feature (e.g., a new condition or target filter), the agent must implement it as a generic, reusable feature in the C# engine (`ConditionEvaluator`, `TargetResolverService`, etc.) and then use it via XML in `ModuleData/Questlines/`.

### 2. Dynamic Negation (!)
The engine features a powerful generic negation system across the board.
- Placing an exclamation mark (`!`) before any Condition or Target Filter automatically inverts its boolean logic.
- Examples: `!HasTroopCount`, `!ClanLeader`, `!SameFactionAs(Alias)`, `!Target(TargetAlias)`.

### 3. Dynamic UI Tracking (Exclamation Marks)
Instead of manually defining which targets to track, `DynamicQuest.cs` dynamically parses the `LogText` of the active stage. If a target's alias is mentioned (e.g., `{TargetAlias_Link}`), the engine automatically calls `AddTrackedObject()` to place the blue quest marker on that hero or settlement.

### 4. Target Resolving & Terminology
Target filters have been strictly standardized to avoid confusion and bugs:
- **`KingdomLeader`**: Strictly targets the ruler (King/Emperor) of a Kingdom.
- **`ClanLeader`**: Targets any standard Clan Leader.
- **`Artisan`, `RuralNotable`, `Merchant`, `Headman`, `GangLeader`**: Matches `Hero.CharacterObject.Occupation` (no "Is" prefix).
- **`Female`, `Male`**: Gender filters.
- **`RequiredSettlementAlias`**: Strictly enforced in `ResolveHero` to ensure NPCs are picked from the exact referenced town/village.

### 5. Dialogue Injection
When injecting dialogues for generic NPCs (like Artisans or Tavern Keepers in towns), avoid relying on `SpeakerStringId` as Bannerlord generates generic StringIds (e.g., `sp_artisan`). Instead:
1. Define a `Resolver` for the NPC (e.g., `Alias="TargetNPC" Filters="Artisan" RequiredSettlementAlias="TargetTown"`).
2. Bind the dialogue to that alias using `TargetAlias="TargetNPC"` in the XML.

## Strict Development Rules

### DO NOT USE POWERSHELL TO EDIT CODE
- AI Agents must **NEVER** use terminal commands (like PowerShell `replace` or Python scripts via CLI) to modify code files.
- Command-line edits bypass the IDE's history/undo tracking and frequently mangle Turkish UTF-8 encodings.
- **Always** use the provided Antigravity tools (`replace_file_content`, `write_to_file`).

### Terminology & Localization
- Ensure English is used for the C# framework terminology and code logic.
- Quest-specific XML content should be written in the intended localization language with flavorful, narrative-driven storytelling.
- Never use the `Is` prefix in generic filter names in the XML (e.g., use `Artisan`, not `IsArtisan`).

### Version Control
- **Always** execute `git add .`, `git commit -m "..."`, and `git push` after every successful build that introduces a working feature or fix. Keep the repository up-to-date with your progress.
