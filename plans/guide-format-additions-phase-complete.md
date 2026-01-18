## Phase 1-6 Complete: Guide Format Additions - Full Implementation

All phases have been implemented in a single development session. The mod now supports nested collapsible dropdowns, custom icons with animation, Table of Contents with scroll-to navigation, and inline image insertion.

**Files created:**
- [src/UI/IconAnimator.cs](../mod/src/UI/IconAnimator.cs) - Animated icon transitions with scale "pop" effect
- [src/UI/TocLinkHandler.cs](../mod/src/UI/TocLinkHandler.cs) - TOC click handling and scroll-to-section navigation
- [plans/guide-format-additions-plan.md](guide-format-additions-plan.md) - Development plan
- mod/images/ - Directory for custom icons and inline images
- mod/update_test_entry.py - Helper script for updating test JSON

**Files changed:**
- [src/Data/Models.cs](../mod/src/Data/Models.cs) - Added new properties to OperationalDetail and DeviceDescriptions
- [src/Harmony/Patches.cs](../mod/src/Harmony/Patches.cs) - Complete refactor for nested categories, TOC, and images
- [StationpediaAscended.cs](../mod/StationpediaAscended.cs) - Added icon loading and image utility methods
- descriptions.json - Updated test entry with all new features

**Functions created/changed:**

New in Models.cs:
- `OperationalDetail.collapsible` - Boolean to make section a collapsible category
- `OperationalDetail.tocId` - ID for TOC linking and scroll-to navigation
- `OperationalDetail.imageFile` - Relative path to inline image
- `OperationalDetail.backgroundColor` - Custom hex color for section background
- `DeviceDescriptions.generateToc` - Enable TOC generation
- `DeviceDescriptions.tocTitle` - Custom TOC title
- `DeviceDescriptions.operationalDetailsBackgroundColor` - Global background color

New in Patches.cs:
- `ApplyCustomCategoryIcons()` - Applies custom expand/collapse icons
- `MonitorCategoryState()` - Coroutine to trigger icon animation on state change
- `CreateTableOfContents()` - Generates TOC panel with clickable links
- `BuildTocLinks()` - Recursively builds TOC link text
- `RenderAllOperationalDetails()` - Entry point for rendering
- `RenderOperationalDetailElement()` - Handles collapsible vs inline rendering
- `CreateNestedCollapsibleCategory()` - Creates nested StationpediaCategory
- `ConfigureNestedCategoryLayout()` - Sets up layout for nested categories
- `CreateInlineContent()` - Creates non-collapsible text content
- `CreateTextElement()` - Helper to create TMP text
- `CreateInlineImage()` - Loads and displays PNG images

New in StationpediaAscended.cs:
- `LoadCustomIcons()` - Loads icon_expanded.png and icon_collapsed.png
- `LoadSpriteFromFile()` - Helper to load PNG as Unity Sprite
- `LoadImageFromModFolder()` - Public API for loading images from mod folder

New Components:
- `IconAnimator` - MonoBehaviour for animated icon transitions
- `TocLinkHandler` - MonoBehaviour for TOC click handling

**Tests created/changed:**
- Comprehensive test entry for ThingStructureSolidFuelGenerator in descriptions.json

**Review Status:** Self-reviewed during implementation

**Git Commit Message:**
```
feat: Add advanced guide formatting features

- Implement nested collapsible dropdowns using StationpediaCategory
- Add IconAnimator component with scale animation on expand/collapse
- Create Table of Contents with clickable scroll-to navigation
- Support inline PNG images in operational details
- Add configurable background colors (Stationeers blue default)
- Update data models with collapsible, tocId, imageFile properties
- Create comprehensive test entry demonstrating all features
```

**New JSON Schema Properties:**

For `OperationalDetail`:
```json
{
  "collapsible": true,      // Make this a collapsible dropdown
  "tocId": "section-id",    // ID for TOC linking
  "imageFile": "path.png",  // Inline image from images folder
  "backgroundColor": "#hex" // Custom background color
}
```

For `DeviceDescriptions`:
```json
{
  "generateToc": true,              // Enable TOC generation
  "tocTitle": "Custom Title",       // TOC panel title
  "operationalDetailsBackgroundColor": "#hex"  // Global background
}
```

**Usage Example:**
```json
{
  "deviceKey": "ThingStructureSolidFuelGenerator",
  "generateToc": true,
  "tocTitle": "Quick Navigation",
  "OperationalDetails": [
    {
      "title": "Fuel Types",
      "tocId": "fuels",
      "collapsible": true,
      "children": [
        {
          "title": "Coal",
          "tocId": "coal",
          "collapsible": true,
          "description": "Primary fuel source"
        }
      ]
    }
  ]
}
```
