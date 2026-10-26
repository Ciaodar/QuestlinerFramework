---
name: mb2-mcp
description: Use MB2-MCP server tools for Mount & Blade II Bannerlord module inspection, XML XSD schema validation, localization scanning and translation management (EN/TR), XML object search/editing, XSLT patch generation/preview, scene/prefab inspection, and game log reading.
---

# MB2-MCP Skill (`mb2-mcp`)

`MB2-MCP` (`https://github.com/RichardBennyVolt/MB2-MCP`) is an MCP server installed at `c:\Users\meteh\RiderProjects\MB2-MCP` that indexes all 56 modules in `D:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord` (official, Workshop, and our custom modules `QuestlinerFramework`, `DemirAhit`, `ShadowsAscendancy`, `ScavengerPigeons`, `JGUM`, `SellMeSlave`, `NaN`).

## Key Tool Groups & Workflows

### 1. Module Validation & Schema Inspection (`core`)
Particularly valuable for **Müfettiş (`Mufettis`)**, **Kod Çalışanları (`KodCalisani`)**, and **Mimarlar**:
- `game_info`: Shows detected Bannerlord install (`v1.4.8`), module folders, writable modules, enabled launcher mods, and index status.
- `list_modules`: Lists installed modules (`filter`: `all` | `official` | `mods` | `writable` | `workshop` | `enabled`).
- `module_info`: Inspects a module's `SubModule.xml`, dependencies, DLLs, `<Xmls>` registrations, unregistered XML files, and language files.
- `validate_module`: Validates a module like the game engine does: `SubModule.xml` (IDs, versions, dependencies, DLLs), registered XMLs against `.NET` XSD schemas, XSLT compilation, duplicate IDs, broken references (`Item.x`, `NPCCharacter.x`), unregistered files, and localization coverage!
- `describe_schema`: Explains valid XML elements and attributes from the game's `XmlSchemas` XSD files plus observed official values.
- `search_objects` / `get_object` / `find_references` / `list_types`: Search across all 25,000+ XML objects (`Item`, `NPCCharacter`, `Hero`, `Faction`, `Kingdom`, `Settlement`, `Culture`, `PartyTemplate`, `CraftingPiece`) and find reverse references.

### 2. Localization Auditing & Editing (`l10n`)
- `l10n_scan`: Scans a module's XML data, GUI prefabs, and C# code for `{=id}Text` strings. Reports missing IDs, empty IDs, conflicting IDs reused with different texts, and translation coverage per language (`EN`, `TR`, etc.).
- `l10n_status`: Lists a module's strings for a target language (`TR`, `EN`, etc.) — `untranslated`, `translated`, `all`, or `stale`.
- `l10n_set`: Writes or updates translations in `ModuleData/Languages/<folder>/` while automatically checking for `{PLACEHOLDER}` mismatches.

### 3. Safe XML & SubModule Editing (`edit`)
- `create_module` / `edit_submodule`: Scaffold or update `SubModule.xml` dependencies, DLLs, and `<XmlNode>` registrations.
- `write_object` / `clone_object` / `set_attributes` / `delete_object`: Safely create, clone, or modify Bannerlord XML objects with automatic XSD validation and backup history (`edit_history`, `undo_edit`).
- `xslt_patch` / `xslt_preview`: Create and preview XSLT patches using the game's native `XslCompiledTransform` pipeline.

### 4. Scenes, Prefabs, Assets & Logs (`scene`, `assets`, `logs`)
- `scene_list`, `scene_info`, `scene_entities`, `scene_edit`, `scene_create`, `scene_settings`: Inspect and edit `scene.xscene` files and spawn points.
- `prefab_search`, `prefab_create`: Search 7,300+ prefabs across modules.
- `asset_search`, `asset_status`, `asset_import`: Search `.tpac` asset packages (meshes, materials, textures, animations, skeletons, physics shapes).
- `read_logs`: Read Bannerlord `rgl_log_*.txt` crash and runtime logs from `C:\ProgramData\Mount and Blade II Bannerlord\logs`.
