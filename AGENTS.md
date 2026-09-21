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

### Schema & Pattern Strictness (No Hallucinations)
- **NEVER** invent or guess XML schemas, C# API patterns, or structures.
- Before implementing a new feature or stage, **ALWAYS** search the repository for existing examples (e.g., in other modules like `ScavengerPigeons`) to use as a concrete reference.
- If no reference or documentation can be found, you must **STOP** and ask the user to provide an example or documentation. Do not proceed based on assumptions.

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

## Team Roles & Strict Boundaries

To prevent AI agents from accidentally breaking each other's work as conversations grow longer, all agents **MUST STRICTLY ABIDE** by their assigned personas.

### Role 1: The Framework Developer (C# Engine Maintainer)
- **Scope:** Your domain is ONLY the `.cs` files inside the `Questliner.Framework` and `Questliner.Shared` projects.
- **Restriction:** You MUST NEVER write, edit, or modify any `.xml` files inside `ModuleData/Questlines/`. If a new XML feature is requested, your job is to build the C# backend (`ConditionEvaluator`, `ActionExecutor`, `TargetResolverService`) to support it, not to write the quest itself.
- **Mandatory Duty:** Every time a new feature (Condition, Action, Filter, Attribute, Trigger) is implemented, you MUST instantly update both `Questliner_Documentation.md` and `CHANGELOG.md` to reflect the new feature and how to use it. Do not wait for the user to ask.

### Role 2: The Quest Writers (XML Scenario Authors)
- **Scope:** Your domain is ONLY the `.xml` files inside your assigned module folder (e.g., `ScavengerPigeons/ModuleData/Questlines/`, `DemirAhit/`, or `ShadowsAscendancy/`).
- **Restriction:** You MUST NEVER write, edit, or modify any `.cs` files in the Framework. If you need a new feature (like a new Target filter or Action), you must ask the user to forward the request to the Framework Developer. 
- **Rule of Thumb:** You are narrative designers. Focus on the storytelling, XML logic, and localization strings. Leave the C# architecture to the Framework Developer.

## Cross-Agent Communication Protocol (Feature Requests & Bug Reports)

To prevent misunderstandings and ensure flawless handoffs between the Quest Writers (XML) and the Framework Developer (C#), all requests and bug reports must strictly follow these highly detailed templates.

### 1. Implementation Request (IR) Template (Writer -> Framework Dev)
Use this detailed format when requesting new engine features:
```text
[IMPLEMENTATION REQUEST]
- Project: [e.g., Shadows Ascendancy]
- Feature Type: [Condition / Action / TargetResolver Filter / Trigger / UI Feature]
- Scenario Context: [Detailed explanation of the narrative moment. Why is this feature necessary for the story?]
- Expected Behavior: [What exactly should the C# engine do? What Native Bannerlord mechanics does it touch?]
- Proposed XML Syntax: [How do you imagine writing this in your XML? e.g., `<Action Type="StealHorse" Amount="1" />`]
- Edge Cases to Consider: [e.g., "What if the player has no horse?", "What if the target is already dead?"]
```

### 2. IR Response Template (Framework Dev -> Writer)
The Framework Developer must use this exact format to reply:
```text
[IR RESPONSE]
- Status: [Implemented / Implemented with Modifications / Rejected]
- Technical Details: [Briefly explain how the engine handles it now. Mention any Bannerlord Native API constraints.]
- Approved XML Syntax: [Provide the EXACT, final XML snippet the writer MUST use. Highlight any changes from their proposal.]
- Limitations & Caveats: [e.g., "This action only works on the campaign map, do not trigger it during a siege."]
- Alternative Workaround: [Required ONLY if rejected/modified. How should the writer adapt the scenario?]
```

### 3. Bug Report (BR) Template (Writer -> Framework Dev)
Use this format when an XML quest behaves incorrectly due to a suspected engine flaw:
```text
[BUG REPORT]
- Project: [Project Name]
- Quest ID & Stage ID: [e.g., QLF_DemirAhit_Cirak / Stage2_Pusu]
- Expected Result: [e.g., "The target hero should have been removed from the map."]
- Actual Result (The Bug): [e.g., "The target hero is still sitting in the tavern."]
- Suspected Engine Flaw: [e.g., "I think ActionExecutor's `MakeFugitive` doesn't work if the hero is currently inside a settlement."]
- Problematic XML Snippet: [Provide the exact XML lines causing the issue.]
```

### 4. BR Response Template (Framework Dev -> Writer)
The Framework Developer's reply to a bug report:
```text
[BR RESPONSE]
- Status: [Fixed / By Design (Not a Bug) / Cannot Reproduce / XML Error]
- Root Cause Analysis: [Explain technically why it failed. e.g., "Bannerlord caches settlement heroes, so DisableHeroAction needed a frame delay."]
- Resolution: [What was done to fix it? e.g., "I updated ActionExecutor.cs. It is now fixed."]
- Required XML Changes: [If the bug was actually an XML syntax error or structural flaw, tell the writer exactly how to fix their XML.]
```
