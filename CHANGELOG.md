# Changelog

All notable changes to Stationpedia Ascended will be documented in this file.

## [0.8.0] - 2026-01-18

### 📝 Station Notepad - In-Game Note Taking
A complete note-taking system integrated into Stationpedia:
- **Multi-Folder Organization** - Create folders and subfolders to organize notes
- **Global & Per-Save Notes** - Notes can persist across all saves or be save-specific
- **Rich Text Formatting** - Bold, italic, headers, colors, bullet points
- **Link to Stationpedia** - Use `{LINK:DeviceKey;Display Text}` to create clickable links to any Stationpedia page
- **Block Editor Mode** - Visual block-based editing with drag-and-drop
- **Text Editor Mode** - Direct markdown-style text editing
- **Auto-Save** - Notes save automatically when switching or closing
- **Keyboard Shortcuts** - F2 to toggle, Escape to close
- **Formatting Toolbar** - Quick access to bold, italic, headers, colors, and lists

### 📚 Massive Content Expansion
- **Survival Manual** - Complete multi-part beginner's guide covering first 5 minutes to advanced systems
- **Power Systems Guide** - Comprehensive guide to electrical networks, solar, batteries, and troubleshooting
- **Airlock Guide** - Basic and advanced airlock setup with component lists and configuration
- **Air Conditioning Guide** - From portable AC safety to full climate control systems
- **Daylight Sensor Guide** - Solar tracking and automation
- **Species Survival Guide (Game Mechanic)** - Human, Zrilian, and Robot survival requirements from game code
- **Complete Smelting Guide (Game Mechanic)** - All furnace types, recipes, temperatures, and alloy creation including Ice Only Recipes and Gaseous Fuel Recipes tables

### 🎨 Table of Contents Enhancements
- **Nested TOC Support** - Multi-level table of contents with collapsible sections
- **Custom TOC Bullets** - Visual hierarchy with different bullet styles for nested items
- **Smooth Scrolling** - Click TOC items to smoothly scroll to sections
- **Auto-Generated IDs** - `tocId` property for precise section linking

### 🎬 Media Support
- **Video Embedding** - Play MP4 videos directly in Stationpedia pages using `videoFile` property
- **Image Support** - Inline images with `imageFile` property
- **YouTube Links** - Clickable YouTube links that open in browser

### 🔄 Vanilla Mode & Ascended Mode
- **Vanilla by Default** - Mod starts in vanilla Stationpedia styling
- **Easter Egg Toggle** - Click the "Stationpedia" header to toggle Ascended mode
- **Visual Distinction** - Ascended mode shows orange styling and phoenix icon
- **Mode Persistence** - Your mode preference is remembered

### 📋 New JSON Schema Features
**Device-Level Properties:**
- `flatStructure` - Render sections without nesting for simpler guides
- `pageImage` - Display image at top of page
- `buttonColor` - Custom color for guide buttons (blue, orange, green, etc.)
- `sortOrder` - Control order of guides in the category list

**OperationalDetail Properties:**
- `children` - Nested subsections for hierarchical content
- `steps` - Numbered step lists for procedures
- `items` - Bullet point lists
- `table` - Data tables with rows and cells

### 🛠️ Technical Improvements
- **JSON Mechanics Loader** - Separate loading system for game mechanics guides
- **Guide Loader System** - Modular guide loading with support for manuals, guides, and mechanics
- **Home Page Layout Manager** - Custom home page with categorized guide buttons
- **Icon Animator** - Smooth icon transitions for UI elements

### 🐛 Bug Fixes
- Fixed JSON BOM character causing parse errors
- Fixed guide key detection for mixed guideKey/deviceKey schemas
- Improved search indexing for new content types
- Fixed Pause stopping tooltips from appearing

---

## [0.3.0] - 2025-12-31

### 🤖 Comprehensive AIMeE Documentation
- **Full AIMeE Guide** - Added extensive documentation for the AIMeE (ThingRobot) companion robot
- **Mode Explanations** - Detailed descriptions for all AIMeE modes: None, Follow, MoveToTarget, Roam, Unload, PathToTarget, StorageFull
- **IC10 Programming Guide** - Complete guide for programming AIMeE with example code snippets
- **Navigation & Mining** - Documented pathfinding behavior, mining mechanics, and coordinate systems
- **Table of Contents** - Added navigable TOC with clickable links to each section

### 📑 Multi-Column Table of Contents
- **Column Layout** - TOC now displays in columns (max 8 rows per column) instead of a long vertical list
- **Dynamic Columns** - Automatically creates additional columns when entries exceed 8
- **Improved Readability** - Better use of horizontal space for guides with many sections

### 📋 New JSON Schema Features
New properties for building rich, interactive documentation:

**Device-Level Properties:**
- `generateToc` - (boolean) Enable Table of Contents panel at top of Operational Details
- `tocTitle` - (string) Custom title for TOC panel (default: "Contents")
- `operationalDetailsBackgroundColor` - (string) Hex color for section backgrounds

**OperationalDetail Properties:**
- `collapsible` - (boolean) Render as expandable/collapsible section with header bar
- `tocId` - (string) Unique ID for TOC linking; clicking scrolls to section
- `imageFile` - (string) Display inline image from mod/images/ folder
- `videoFile` - (string) Embed MP4 video player from mod/images/ folder
- `youtubeUrl` - (string) Clickable YouTube link that opens in browser
- `youtubeLabel` - (string) Custom label for YouTube link
- `backgroundColor` - (string) Custom hex color for individual section

### 🎨 Vanilla Mode Default & Easter Egg Toggle
- **Vanilla by Default** - Mod now starts in vanilla Stationpedia styling mode
- **Header Toggle** - Click the "Stationpedia" header to toggle Ascended mode (easter egg)
- **Visual Feedback** - Header changes to "Stationpedia Ascended" with orange styling when enabled
- **Icon Swap** - Custom phoenix icon only appears in Ascended mode
- **Removed Book Button** - Simplified UI by removing the mode toggle button

### 🔧 Bug Fixes
- **Fixed JSON Syntax Error** - Resolved corrupted line in descriptions.json that broke tooltip parsing
- **Fixed Brace Mismatch** - Corrected code structure issues in icon replacement logic

### 📝 New Files
- `VanillaModeManager.cs` - Static manager for vanilla/ascended mode state
- `CategoryHeaderHandler.cs` - Handler for category header interactions
- `LLM_INSTRUCTIONS.txt` - Completely rewritten documentation for JSON schema

---

## [0.2.2] - 2025-12-30

### ⚡ Search Performance Optimizations
- **O(1) Title Index Lookups** - Built cached title/word indexes for instant lookups instead of O(n) full scans
- **Reduced Search Latency** - Replaced 1-second polling loop with 2-frame delay for near-instant results
- **Category Cache** - Added category lookup cache to avoid triple-nested loops during reorganization
- **Template Caching** - Cached font, sprite, and color references to avoid repeated lookups

### 🏷️ New Property Tooltips
Hover over gas, material, and device properties to see detailed explanations:
- **Gas/Material Properties**: Flashpoint, Autoignition, Thermal Convection, Thermal Radiation, Solar Heating, Specific Heat, Freeze Temperature, Boiling Temperature, Max Liquid Temperature, Min Liquid Pressure, Latent Heat, Moles Per Litre
- **Device Properties**: Max Pressure, Volume, Base Power, Power Storage, Power Generation
- **Plant/Food Properties**: Growth Time, Nutrition, Nutrition Quality, Mood Bonus
- **Rocket Properties**: Placeable In Rocket, Rocket Mass

### 🎮 IC10 Helper Tooltips
- **Prefab Name Tooltip** - Shows full name (in case truncated), "click to copy" instruction, and IC10 usage example
- **Prefab Hash Tooltip** - Shows hash value with IC10 code example (`lb r0 <hash> Setting`)
- Explains that Prefab Name and Hash are interchangeable for IC10 programming

### 🔧 Bug Fixes & Polish
- **Prefab Name Truncation** - Fixed truncation to prevent overlap with Stack Size field
- **Removed Debug Spam** - Cleaned up console logs by removing verbose debug output
- **New Tooltip Classes** - Added `SPDAPrefabInfoTooltip` and `SPDAPropertyTooltip`

---

## [0.2.1] - 2025-12-29

### ✨ Enhanced Search Results
- **Smart Search Organization** - Search results are now intelligently grouped by relevance:
  - **Exact Matches** - Items where the title exactly matches your search (orange header)
  - **Starts With** - Items where the title begins with your search term (gold header)
  - **Category Groups** - Remaining results organized by their Stationpedia category (white headers)
  
- **Visual Header Styling**
  - Beautiful blue-bordered header bars using the game's "special" UI sprite
  - Darkened background for better contrast with colored text
  - Headers are 54px tall with 19px bold text for clear visibility
  
- **Debris Filtering** - Automatically filters out burnt cables, wreckage, and ruptured variants from search results to reduce clutter

- **Priority Scoring System** - Results are scored and sorted by:
  1. Exact title match (highest priority)
  2. Title starts with search term
  3. Title contains search term
  4. Description contains search term (lowest priority)

---

## [0.1.3] - 2025-12-28

### 🔧 Build & Development Fixes
- **Fixed PDB symbol format** - Changed from `portable` to `embedded` format to resolve Mono.Cecil hot-reload exceptions (`SymbolsNotMatchingException` and `MarshalDirectiveException`)
- **Updated build task** - Simplified deployment to only copy DLL and descriptions.json (PDB now embedded in DLL)

### 🏗️ Code Organization
- **Added region markers** - Organized main StationpediaAscended.cs file with 12 collapsible regions for easier navigation:
  - Constants & Static References
  - Instance Fields
  - ScriptEngine Hot-Reload Support
  - Unity Lifecycle Methods
  - GUI Rendering
  - Resource Loading
  - Harmony Patching
  - Console Commands
  - Static ScriptEngine Methods
  - Tooltip Adding
  - Description Lookup Helpers
  - Cleanup

### 📝 Documentation
- Added XML documentation comments to major methods

---

## [0.1.2] - 2025-12-28

### 🏗️ Major Code Refactoring
Restructured the codebase from a single 2,535-line monolithic file into a clean multi-file architecture for improved maintainability and navigation.

#### New File Structure
- **`src/Data/Models.cs`** - All 9 JSON data model classes (`DescriptionsRoot`, `DeviceDescriptions`, `LogicDescription`, `ModeDescription`, `SlotDescription`, `VersionDescription`, `MemoryDescription`, `OperationalDetail`, `GenericDescriptionsData`)
- **`src/Tooltips/SPDABaseTooltip.cs`** - Abstract base class with shared tooltip functionality (hover delay, pointer events, positioning)
- **`src/Tooltips/SPDALogicTooltip.cs`** - Logic type tooltip component
- **`src/Tooltips/SPDASlotTooltip.cs`** - Slot tooltip component
- **`src/Tooltips/SPDAVersionTooltip.cs`** - Version tooltip component  
- **`src/Tooltips/SPDAMemoryTooltip.cs`** - Memory/register tooltip component
- **`src/Patches/HarmonyPatches.cs`** - All Harmony patch methods (`PopulateLogicSlotInserts_Postfix`, `ChangeDisplay_Postfix`, `OnDrag_Prefix`, `OnBeginDrag_Prefix`) plus `CreateOperationalDetailsCategory` helper
- **`src/Core/TooltipState.cs`** - Centralized tooltip visibility state management

#### Code Improvements
- Reduced main file (`StationpediaAscended.cs`) from 2,535 lines to ~1,500 lines (43% reduction)
- Eliminated code duplication in tooltip classes via shared base class
- Improved separation of concerns between data models, UI components, and patches
- Added proper namespace organization (`StationpediaAscended.Data`, `StationpediaAscended.Tooltips`, `StationpediaAscended.Patches`, `StationpediaAscended.Core`)

### 🐛 Bug Fixes
- **Fixed JSON deserialization for OperationalDetails** - Added `[JsonProperty]` attributes to handle case-sensitivity between JSON (`"OperationalDetails"`) and C# (`operationalDetails`)
- **Fixed descriptions.json path resolution** - Updated hardcoded development paths from old `StationpediaPlus` folder to `StationpediaAscended`
- **Fixed null reference in path resolution** - Added null-safety for `Path.GetDirectoryName()` which can return null
- **Fixed VS Code build task** - Updated `.vscode/tasks.json` to use correct project name and paths

### 🔧 Technical Changes
- Renamed `StationpediaAscended.Harmony` namespace to `StationpediaAscended.Patches` to avoid conflict with `HarmonyLib.Harmony`
- Added `using Newtonsoft.Json;` import to Models.cs for JsonProperty attributes
- Improved console logging during JSON path search for easier debugging
- Build task now correctly deploys `StationpediaAscended.dll` instead of `StationpediaPlus.dll`

---

## [0.1.0-beta] - 2025-12-28

### 🎉 Initial Release (Beta)
- **Renamed** from StationpediaPlus to **Stationpedia Ascended**
- **Custom Phoenix Icon** - Replaced the original book icon with a custom phoenix logo in the header

### ✨ New Features
- **Enhanced Tooltips System**
  - Added comprehensive tooltips for logic descriptions
  - Added slot-specific tooltips with detailed information
  - Added memory/register tooltips explaining functionality
  - Added mode descriptions for device operations
  - Added connection type explanations
  - JSON-based configuration system for easy customization

- **Operational Details Section**
  - Added dedicated "Operational Details" category that appears at the top of device pages
  - Configurable title color via JSON configuration
  - Phoenix icon displayed next to the category title
  - Defaults to collapsed state for cleaner initial view
  - Automatically positioned after the main description section

- **Page Description Customization**
  - Added ability to completely replace page descriptions
  - Added `pageDescriptionAppend` for adding content after existing descriptions
  - Added `pageDescriptionPrepend` for adding content before existing descriptions
  - Full JSON configuration support via `descriptions.json`

### 🐛 Bug Fixes
- **Fixed Critical Scrollbar Handle Bug**
  - Resolved issue where scrollbar handles would disappear on non-home Stationpedia pages
  - Implemented 5-frame delayed fix to combat handle position corruption
  - Handle `localPosition` and `anchoredPosition` now properly reset to zero
  - Bug affects base game - created detailed bug report for developers

- **Fixed Window Dragging Crash**
  - Resolved crash when dragging Stationpedia window in main menu
  - Fixed `ClampToScreen()` null reference exception due to missing `InventoryManager.ParentHuman`
  - Replaced problematic dragging logic with simple position assignment

### 🔧 Technical Improvements
- **Harmony Integration**
  - Patches `ChangeDisplay` method for enhanced functionality
  - Patches `PopulateLogicSlotInserts` for tooltip integration
  - Patches `OnDrag` and `OnBeginDrag` for improved window handling
  - Hot-reload support for development workflow

- **Performance Optimizations**
  - Coroutine-based monitoring system for Stationpedia state
  - Efficient component cleanup on mod reload
  - Delayed tooltip application to prevent UI conflicts

- **Developer Experience**
  - ScriptEngine hot-reload compatibility (F6 reload support)
  - Comprehensive debug logging system
  - Automatic file path detection for development and production environments

### 📝 Documentation
- Created comprehensive bug report document for game developers
- Added detailed JSON configuration examples
- Documented all tooltip categories and customization options

### 🎨 UI/UX Improvements
- **Custom Branding**
  - Window title changed to "Stationpedia Ascended" with orange accent color
  - Phoenix icon properly sized and positioned in header
  - Maintains original UI layout and responsiveness

- **Enhanced Information Display**
  - Tooltips appear on hover with orange border styling
  - Consistent formatting across all tooltip categories
  - Non-intrusive design that complements existing UI

### ⚙️ Configuration
- **descriptions.json Structure**
  ```json
  {
    "genericDescriptions": {
      "logic": { "key": "description" },
      "slots": { "key": "description" },
      "memory": { "key": "description" },
      "modes": { "key": "description" },
      "connections": { "key": "description" }
    },
    "devices": {
      "DevicePageKey": {
        "operationalDetails": [...],
        "operationalDetailsTitleColor": "#FF7A18",
        "pageDescription": "Complete replacement",
        "pageDescriptionAppend": "Added after existing",
        "pageDescriptionPrepend": "Added before existing"
      }
    }
  }
  ```

### 🔄 Compatibility
- Compatible with Stationeers current version
- Works with both BepInEx and ScriptEngine loading methods
- Hot-reload support for development
- No conflicts with existing Stationpedia functionality

---

*Note: This is a beta release. Please report any issues or feedback on the GitHub repository.*