# Changelog

All notable changes to Stationpedia Ascended will be documented in this file.

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