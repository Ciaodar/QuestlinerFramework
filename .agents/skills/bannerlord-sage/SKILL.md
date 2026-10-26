---
name: bannerlord-sage
description: Use BannerlordSage MCP server tools for Mount & Blade II Bannerlord C# decompiled source search, type definitions, Gauntlet UI inspection, official/community modding documentation search, structured gameplay lookups (troops, items, heroes, clans, kingdoms, settlements, skills, perks, policies), local mod source indexing/search, and persistent project memory.
---

# BannerlordSage MCP Skill (`bannerlordsage`)

`BannerlordSage` (`https://github.com/hby7921/BannerlordSage`) is an MCP server installed at `c:\Users\meteh\RiderProjects\BannerlordSage` that indexes our local Mount & Blade II: Bannerlord installation (`D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord`), decompiles all official TaleWorlds DLLs, builds a Roslyn + SQLite C# & XML index, and indexes our local mod source (`c:\Users\meteh\RiderProjects\QuestlinerFramework`).

## Tool Groups & When to Use Them

### 1. Official C# Source, Types & Gauntlet UI
Use these tools whenever researching Native Bannerlord C# mechanics, campaign behaviors, models, or UI bindings:
- `search_source`: Ripgrep full-text search across all decompiled official Bannerlord DLLs.
- `read_csharp_type`: Read the exact decompiled C# definition of a TaleWorlds class/struct/interface/enum by name (e.g. `QuestBase`, `Settlement`, `Hero`, `PlayerEncounter`, `CampaignEvents`).
- `read_file`: Read a specific line range from an indexed official source or XML file.
- `list_directory`: Browse the imported official asset directory.
- `read_gauntlet_ui`: Inspect Gauntlet UI movie bindings and ViewModel (`ViewModelCollection`) interaction logic.

### 2. Official XML & Localization Lookup
- `search_xml`: Search official XML files for an ID, field, token, or concept.
- `resolve_localization`: Resolve in-game localization tokens like `{=abc123}` to actual text.

### 3. Official/Community Documentation & API Search
- `search_bannerlord_knowledge`: Unified natural-language search across official/community docs, API symbols, and game knowledge.
- `search_bannerlord_docs`: Search official modding tutorials and BannerlordModding.LT community documentation.
- `search_bannerlord_api_docs`: Search official API Doxygen symbols.

### 4. Structured Gameplay Data Lookups
Use these when designing quests, balancing rewards, or checking vanilla IDs:
- `trace_troop_tree`: Query the full upgrade tree for a troop ID.
- `get_item_stats`: Query detailed weapon, armor, horse, or smithing piece stats.
- `get_hero_profile`: Query a hero's skills, traits, clan, and background.
- `get_clan_summary`: Query a clan's members, tier, culture, and relations.
- `get_kingdom_summary`: Query a kingdom's territories, policies, and clans.
- `get_culture_summary`: Query a culture's units, bonuses, and traits.
- `get_settlement_summary`: Query a town, castle, or village's owner, culture, bound villages, and prosperity.
- `get_skill_data`, `get_perk_data`, `get_policy_summary`: Query skills, perks, and kingdom policies.

### 5. Local Mod Source Indexing & Search
- `mod_source_status`: Check whether a local mod source workspace is configured and indexed (`QuestlinerFramework` by default, or pass `workspaceRoot` for `JustGiveUpMan`, `SellMeSlave`, `Not-a-Noble`).
- `index_mod_source`: Re-index a local mod source workspace (`workspaceRoot`) after C# changes.
- `search_mod_source`, `read_mod_file`, `list_mod_directory`, `read_mod_type`: Search and read C# types in our own mod projects.

### 6. Project Memory Workflow
- `project_memory_wakeup`: Load key active memory for a workspace at session start.
- `project_memory_search`: Search project memory before answering questions about past architectural decisions or pitfalls.
- `project_memory_add` / `project_memory_capture_session`: Store durable decisions, pitfalls, preferences, and TODOs.
- `project_memory_recent` / `project_memory_invalidate`: Inspect recent memories or invalidate outdated ones.

### 7. Diagnostics & Scaffolding
- `bannerlord_doctor`: Check module health (missing dependencies, duplicate DLLs, load order issues).
- `bannerlord_index_status`: Verify BannerlordSage index readiness.
- `generate_harmony_patch`, `create_mod_workspace`, `generate_xslt_patch`: Generate scaffolding for Harmony patches, new modules, or XSLT patches.
