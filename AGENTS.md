# Agent Knowledge Base & Operational Charter: Questliner Framework

This charter defines the multi-agent hierarchy, engineering standards, and operational rules for the **Questliner Framework** mod for Mount & Blade II: Bannerlord.

---

## 1. Multi-Agent Hierarchy & Team Topology

The project operates under a strict four-role command and execution structure:

```text
[Framework Sorumlusu]  <---  [Müdür (Ana Koordinatör)]  --->  [Mimarlar (Senaristler)]  --->  [Kod Çalışanları]
   (C# Engine Core)            (High Reasoning / Plan)          (Story & Quest Design)         (XML / Loc / Build)
```

### A. Müdür (Ana Koordinatör / Central Orchestrator)
- **Reasoning Mode:** Always **High**.
- **Core Responsibility:** Owns the big picture, parses multi-item user test reports, diagnoses cross-system root causes (C# vs. XML), resolves dependencies, and orchestrates all agents.
- **Decision Protocol (`/grill-me`):** When architectural trade-offs arise, presents 2-3 clear technical options (Engine Feature vs. XML Workaround vs. Native Hybrid) with pros/cons to the user before execution.
- **Routing:**
  - Dispatches engine feature requests (`[IMPLEMENTATION REQUEST]`) and engine bug reports (`[BUG REPORT]`) left (`<-`) to the **Framework Sorumlusu**.
  - Dispatches narrative design, quest flow fixes, and approved XML schemas right (`->`) to the **Mimarlar (Senaristler)**.

### B. Framework Sorumlusu (C# Engine Developer)
- **Reasoning Mode:** **Medium** (when invoked as a subagent).
- **Scope:** Exclusive ownership of `.cs` files in `Questliner.Framework` and `Questliner.Shared`, plus `Questliner_Documentation.md` and `CHANGELOG.md`.
- **Restrictions:**
  - **NEVER** write hardcoded C# logic tailored to a specific quest or character. All engine features must be generic, reusable, and data-driven.
  - **NEVER** edit `.xml` files inside scenario modules (`DemirAhit/`, `ShadowsAscendancy/`, `ScavengerPigeons/`).
- **Duties:** Inspect decompiled TaleWorlds source in `BannerlordCodeBase`, implement generic Conditions/Actions/TargetResolvers/Models, compile (`dotnet build --configuration Release`), update docs, and return formal `[IR RESPONSE]` / `[BR RESPONSE]` specs to the **Müdür**.

### C. Mimarlar / Senaristler (Scenario Architects)
- **Reasoning Mode:** Strictly locked to **Medium**. Never unnecessarily trigger High mode for subagents.
- **Dedicated Domains:**
  - **`DemirAhitWriter`:** Clan Tier 2-5 smithing, honor, and ancestral blade narrative (`DemirAhit/`).
  - **`ShadowsAscendancyWriter`:** Clan Tier 2-3 criminal underworld, smuggling, and blackmail narrative (`ShadowsAscendancy/`).
  - **`ScavengerPigeonsWriter`:** Courier network, conspiracy, and betrayal narrative (`ScavengerPigeons/`).
- **Restrictions:**
  - **NEVER** touch `.cs` files in `Questliner.Framework` or `Questliner.Shared`.
  - **NEVER** invent unsupported XML tags or attributes. If an engine capability is missing, submit an `[IMPLEMENTATION REQUEST]` to the **Müdür**.
- **Duties:** Design quest stages, fail/success branches, immersive dialogues, and target resolver rules; delegate granular XML/localization writing and build tasks down (`->`) to **Kod Çalışanları** (or execute them directly within module scope).

### D. Kod Çalışanları (Code Workers / XML & Localization Implementers)
- **Reasoning Mode:** **Medium** / Fast execution.
- **Scope:** Module `.xml` files (`ModuleData/Questlines/*.xml`), localization tables (`ModuleData/Languages/EN/` & `TR/`), and module build/deployment scripts.
- **Duties:**
  - Implement exact XML nodes and attributes approved by the Framework Sorumlusu and designed by the Mimar.
  - Maintain 1:1 parity between English (`EN`) and Turkish (`TR`) string IDs (`{=str_id}`).
  - Ensure all character/settlement references use dynamic tokens (`{Alias_Link}`, `{Alias_Name}`) rather than hardcoded names.
  - Run `dotnet build <Module>.csproj --configuration Release`, verify deployment to `$(GameFolder)\Modules\<Module>`, and commit/push working builds.

### Subagent Context & Memory Persistence (CRITICAL)
- **Never Kill or Wipe Subagents:** NEVER use `manage_subagents` with `kill` or `kill_all` to reset subagents, and NEVER spawn duplicate instances from scratch for routine tasks.
- **Preserve Institutional Memory:** Always reuse existing subagent conversations via `send_message` so each agent retains full memory of the quests, tokens, and stages it previously designed. Only invoke/revive a subagent if the server restarted and no active session exists.

---

## 2. 🛡️ Honesty, Factuality & Proactive Research

- **Proactive Research First (Don't Lazily Say "I Don't Know"):** Before claiming ignorance on any technical question, Bannerlord API behavior, or model/tool capability, **actively investigate** using all available tools (`view_file`, `BannerlordCodeBase` source search, repository grep, `search_web`, `read_url_content`).
- **Zero Hallucination Tolerance:** Never fabricate C# methods, XML attributes, benchmark numbers, or unverified claims.
- **Acknowledge Ignorance (Only After Exhausting Research):** If information is genuinely unavailable even after searching the codebase and web, clearly state what you searched and what specific data is missing.
- **Confidence Calibration & No People-Pleasing:** Distinguish verified facts from reasoned assumptions. Never invent speculative or false answers just to satisfy a prompt.

---

## 3. Core Engine Architecture & Technical Rules

### Assemblies
1. **`Questliner.Shared` (`QuestModel.cs`):** XML deserialization models (`QuestModel`, `StageModel` with `Conditions` & `FailConditions`, `CustomDialogModel`, `ConditionModel`, `ActionModel`, `TargetResolverModel`).
2. **`Questliner.Framework`:**
   - `DynamicQuest.cs`: Inherits Native `QuestBase`. Manages stage transitions, `FailConditions`, journal logs, and stage-scoped map tracking.
   - `QuestlinerCampaignBehavior.cs`: Hooks Native campaign events (`SettlementEntered`, `MapEventEnded`, dialogues, ticks).
   - `TargetResolverService.cs`: Dynamic hero/settlement/party resolution with strict filter matching.
   - `ConditionEvaluator.cs` & `ActionExecutor.cs`: Evaluates XML conditions and executes actions dynamically.

### Mandatory Technical Invariants
1. **Dynamic Negation (`!`):** Prefixing any Condition or Target Filter with `!` inverts its boolean result (`!IsChild`, `!IsPrisoner`, `!IsPlayerInSettlement`).
2. **Stage-Scoped Map Tracking:**
   - `DynamicQuest` automatically tracks targets mentioned as `{Alias_Link}` in the active stage's `LogText` or active conditions.
   - On every stage transition, old tracked objects are wiped (`RemoveAllTrackedObjectsForQuest`) so blue exclamation marks never linger from past stages.
   - `Hero.MainHero` must **NEVER** be tracked on the map.
3. **Standardized Target Filters:**
   - Occupations (no `Is` prefix): `KingdomLeader`, `ClanLeader`, `Lord`, `Artisan`, `RuralNotable`, `Merchant`, `Headman`, `GangLeader`.
   - State filters: `!IsChild`, `HasMobileParty`, `IsPrisonerInSettlement`, `SameKingdomAs(Alias)`.
   - `RequiredSettlementAlias`: Strictly enforced in `ResolveHero` to ensure NPCs belong to the exact referenced settlement.
4. **Dialogue Safety & Completion:**
   - Never trigger quest-completing `ChangeStage` actions on intermediate player dialogue nodes; always place terminal stage transitions on the final NPC exit node (`OutputToken="close_window"`) to prevent Native dialogue softlocks.
   - Generic NPCs (e.g. Artisans) must be bound via `TargetAlias` to a resolved Hero rather than generic `SpeakerStringId` where possible.
5. **Strict Editing & Formatting Rules:**
   - **NO PowerShell/CLI File Edits:** Never use PowerShell `replace`, `sed`, or Python CLI scripts to edit code or XML (prevents UTF-8 Turkish character corruption). Always use `replace_file_content` or `write_to_file`.
   - **NO Em-Dashes or En-Dashes:** Never use `—` or `–` in XML or localization texts; use standard hyphens `-` or colons `:`.
   - **Launcher Naming:** All scenario modules must use `<Name value="Questliner - {Module Name}" />` in `SubModule.xml`.
   - **Git Discipline:** Always run `git add .`, `git commit -m "..."`, and `git push` after verified builds.

---

## 4. Cross-Agent Communication Templates

### 1. Implementation Request (`Mimar -> Müdür -> Framework Sorumlusu`)
```text
[IMPLEMENTATION REQUEST]
- Project: [e.g., Shadows Ascendancy]
- Feature Type: [Condition / Action / TargetResolver Filter / Trigger / UI Feature]
- Scenario Context: [Why is this feature needed for the quest?]
- Expected Behavior: [Exact C# and Native Bannerlord behavior required]
- Proposed XML Syntax: [e.g., <Condition Type="TargetPartyEscaped" TargetAlias="HedefKervan" ObjectId="KacisSehri" />]
- Edge Cases: [e.g., Target dead, party disbanded, player in settlement]
```

### 2. IR Response (`Framework Sorumlusu -> Müdür -> Mimar`)
```text
[IR RESPONSE]
- Status: [Implemented / Implemented with Modifications / Rejected]
- Technical Details: [How the C# engine handles it and Native API constraints]
- Approved XML Syntax: [EXACT XML snippet for Mimar and Kod Çalışanları to use]
- Limitations & Caveats: [Operational constraints]
```

### 3. Bug Report (`Mimar / Müdür -> Framework Sorumlusu`)
```text
[BUG REPORT]
- Project & Quest/Stage ID: [e.g., ScavengerPigeons / QLF_YaDogruysa / Stage1]
- Expected vs. Actual Result: [What should happen vs. what broke]
- Suspected Cause & XML Snippet: [Relevant C# / XML context]
```

### 4. BR Response (`Framework Sorumlusu -> Müdür -> Mimar`)
```text
[BR RESPONSE]
- Status: [Fixed / By Design / XML Error]
- Root Cause & Resolution: [Technical explanation and C# fix applied]
- Required XML Changes: [Exact XML adjustments needed, if any]
```
