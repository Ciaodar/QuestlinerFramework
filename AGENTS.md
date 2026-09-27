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

## Team Roles & Multi-Agent Architecture

To maintain high architectural integrity and prevent context pollution, the project operates under an **Orchestrator - Subagent (Quest Writers)** model.

### Role 1: The Lead Framework Architect (Central Agent / Orchestrator)
- **Scope:** Complete ownership of `.cs` files in `Questliner.Framework` and `Questliner.Shared`, engine architecture, and subagent coordination.
- **Restriction:** NEVER write or edit `.xml` files inside scenario modules (`DemirAhit/`, `ShadowsAscendancy/`, `ScavengerPigeons/`).
- **Core Duties:**
  - Evaluate incoming feature requests (`[IMPLEMENTATION REQUEST]`) and bug reports (`[BUG REPORT]`) from scenario subagents.
  - Present multi-option technical analyses to the user (`/grill-me` style).
  - Implement C# engine changes, build, verify, and push to GitHub.
  - Immediately update `Questliner_Documentation.md` and `CHANGELOG.md` upon any engine change.
  - Dispatch structured `[IR RESPONSE]` and `[BR RESPONSE]` directly to the respective scenario subagents.

### Role 2: The Scenario Quest Writers (Subagents)
Each storyline has a dedicated subagent with an isolated context:
- **`DemirAhit Subagent`**: Focuses on Clan Tier 2-5 smithing, honor, and loyalty narrative (`DemirAhit/`).
- **`ShadowsAscendancy Subagent`**: Focuses on Clan Tier 2-3 criminal underworld, smuggling, and blackmail narrative (`ShadowsAscendancy/`).
- **`ScavengerPigeons Subagent`**: Focuses on courier network, conspiracy, and betrayal narrative (`ScavengerPigeons/`).
- **Restrictions:**
  - Scope is STRICTLY limited to their designated module's `.xml` files, localization files (`Languages/`), and build scripts.
  - NEVER edit C# files in `Questliner.Framework` or `Questliner.Shared`.
  - NEVER invent unsupported XML tags. If an engine limitation is reached, issue an `[IMPLEMENTATION REQUEST]` to the Lead Framework Architect.
  - Always execute build (`dotnet build`), version increment, and Git push after completing working stages.

---

## Interactive Decision & Execution Protocols

### 1. Technical Analysis & Options Protocol (/grill-me Style)
When a scenario subagent submits an `[IMPLEMENTATION REQUEST]`, `[BUG REPORT]`, or encounters a technical block:
1. The Lead Framework Architect analyzes the problem across Bannerlord Native APIs and the Framework engine.
2. The Architect formulates **2 to 3 distinct technical options** with pros/cons and presents them directly to the User:
   - **Option A (Engine Feature):** Extending the C# engine (cost: compilation, new API, potential edge cases).
   - **Option B (XML Workaround / Pure Narrative):** Adapting the quest logic using existing conditions/actions without touching C#.
   - **Option C (Alternative Mechanic / Hybrid):** Alternative Bannerlord native mechanics (e.g. game menu, dialogue re-route).
3. The Architect asks the User to pick the desired path before writing code.

### 2. Decision & Execution Protocol
Once the User selects an option:
1. **If C# changes are required:** The Lead Framework Architect implements the feature in `Questliner.Framework` / `Questliner.Shared`, derives tests, increments version, updates `Questliner_Documentation.md` and `CHANGELOG.md`, and pushes to Git.
2. **Dispatch Response:** The Architect crafts a formal `[IR RESPONSE]` or `[BR RESPONSE]` with approved XML syntax and directives, then commands the scenario subagent to execute the XML updates.
3. **Subagent Execution:** The scenario subagent updates its XML and localization files, runs its build script, and pushes changes.

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
