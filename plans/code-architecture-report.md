# Stationpedia Ascended - Code Architecture Report

## Executive Summary

The mod's rendering code in `Patches.cs` has evolved organically to support multiple content types (devices, guides, survival manual, game mechanics), resulting in **significant code duplication** across similar functionality. This report identifies the siloed code, analyzes the patterns, and proposes a unified architecture.

---

## Current State: Siloed Rendering Code

### 1. Table of Contents (TOC) Functions

There are **3 separate TOC implementations**, each with slightly different parameters:

| Function | Location | Column Spacing | Row Limit | Column Width | Line Spacing |
|----------|----------|----------------|-----------|--------------|--------------|
| `CreateTableOfContents` | Line 1686 | 20 | 8 | Not set | Not set |
| `CreateGuideTableOfContents` | Line 902 | 24 | 8 | 180 | 8 |
| `CreatePartTableOfContents` | Line 1153 | 30 | 8 | 200 | 8 |

**Key Differences:**
- **Device TOC** (`CreateTableOfContents`): No fixed column width, no line spacing - causes text overlap
- **Guide TOC** (`CreateGuideTableOfContents`): Uses `preferredWidth=180`, `lineSpacing=8`
- **Survival Manual Part TOC** (`CreatePartTableOfContents`): Uses `preferredWidth=200`, word wrapping enabled, ellipsis overflow

**Root Cause of Text Overlap:** The device TOC lacks `lineSpacing` and fixed column widths that the other TOCs have.

---

### 2. Section Rendering Functions

There are **3 parallel sets** of section rendering functions:

#### For Devices (Operational Details):
```
RenderAllOperationalDetails()
  └─> RenderOperationalDetailElement()
        └─> CreateNestedCollapsibleCategory()
        └─> CreateInlineOperationalDetail()
```

#### For Guides:
```
RenderGuideSections()
  └─> RenderGuideSectionElement()
        └─> CreateGuideCollapsibleSection()
        └─> CreateGuideInlineContent()
```

#### For Survival Manual:
```
RenderSurvivalManualPage()
  └─> CreateSurvivalManualPart()
        └─> CreateSurvivalManualSection()
```

**Overlap Analysis:**
- `CreateNestedCollapsibleCategory` and `CreateGuideCollapsibleSection` do essentially the same thing
- `CreateInlineOperationalDetail` and `CreateGuideInlineContent` are nearly identical
- `CreateSurvivalManualSection` is a third implementation of the same pattern

---

### 3. Image Creation Functions

There are **2 image functions** with inconsistent sizing:

| Function | Max Width | Flexible Width | Use Case |
|----------|-----------|----------------|----------|
| `CreateGuideTopImage` | 500px | 1 (stretches) | Guide top images - TOO LARGE |
| `CreateInlineImage` | 300px | 0 (no stretch) | Inline images - correct size |

**Problem:** `CreateGuideTopImage` creates enormous images compared to vanilla Stationpedia. It should use the same size as inline images (300px, no stretching).

---

### 4. Page Rendering Entry Points

There are **4 separate page rendering functions** that share common patterns:

| Function | Content Type | Uses |
|----------|--------------|------|
| `RenderSurvivalManualPage` | Survival Manual | `CreateSurvivalManualPart`, `CreatePartTableOfContents` |
| `RenderJsonGuidePage` | JSON Guides | `RenderGuideSections`, `CreateGuideTableOfContents` |
| `RenderDaylightSensorGuidePage` | Daylight Sensor | `RenderGuideSections` |
| `CreateOperationalDetailsCategory` | Device pages | `CreateTableOfContents`, `RenderOperationalDetailElement` |

---

### 5. Data Loaders

There are **3 separate loader classes**:

| Loader | File | Data Source |
|--------|------|-------------|
| `JsonGuideLoader` | `Data/JsonGuideLoader.cs` | `descriptions.json` guides array |
| `SurvivalManualLoader` | `Data/SurvivalManualLoader.cs` | Markdown file + descriptions.json fallback |
| `DeviceDatabase` | `Data/DeviceDatabase.cs` | `descriptions.json` devices array |

---

## Proposed Unified Architecture

### Phase 1: Unified TOC Rendering

Create a single `CreateTableOfContents` function that accepts a configuration object:

```csharp
public class TocConfig
{
    public int ColumnSpacing { get; set; } = 24;
    public int MaxRowsPerColumn { get; set; } = 8;
    public int MaxColumns { get; set; } = 3;
    public float ColumnWidth { get; set; } = 200f;
    public float LineSpacing { get; set; } = 8f;
    public bool EnableWordWrapping { get; set; } = true;
    public string Title { get; set; } = "Contents";
}

private static void CreateUnifiedTableOfContents(
    RectTransform parent, 
    TMPro.TextMeshProUGUI sourceText, 
    List<(string tocId, string title, int depth)> entries,
    TocConfig config)
{
    // Single implementation with consistent behavior
}
```

**Benefits:**
- Single source of truth for TOC rendering
- Consistent spacing and styling across all page types
- Configuration allows per-context customization

---

### Phase 2: Unified Section Rendering

Create a single section rendering system:

```csharp
public enum SectionStyle
{
    Collapsible,    // With expand/collapse
    Inline,         // Flat content
    Part            // Survival manual Part style
}

private static void RenderSection(
    RectTransform parent,
    TMPro.TextMeshProUGUI sourceText,
    OperationalDetail detail,
    StationpediaCategory categoryPrefab,
    UniversalPage page,
    int depth,
    SectionStyle style)
{
    // Single implementation handling all section types
}
```

**Current Duplication to Eliminate:**
- `CreateNestedCollapsibleCategory` → merged
- `CreateGuideCollapsibleSection` → merged
- `CreateSurvivalManualSection` → merged
- `CreateInlineOperationalDetail` → merged
- `CreateGuideInlineContent` → merged

---

### Phase 3: Unified Image Rendering

Create a single image function with size constants matching vanilla:

```csharp
private const float STANDARD_IMAGE_WIDTH = 300f;  // Match vanilla Stationpedia

private static void CreateImage(
    RectTransform parent, 
    string imageFile, 
    bool preserveAspect = true,
    float? maxWidth = null)  // Defaults to STANDARD_IMAGE_WIDTH
{
    // Single implementation
}
```

**Remove:** `CreateGuideTopImage` (merge into `CreateImage`)

---

### Phase 4: Unified Page Rendering

Create a single entry point for rendering custom content:

```csharp
public enum PageType
{
    Device,
    Guide,
    SurvivalManual,
    GameMechanic
}

private static void RenderCustomContent(
    UniversalPage page,
    Transform contentTransform,
    DeviceDescriptions data,
    PageType pageType)
{
    // Dispatch to unified rendering with appropriate config
}
```

---

## Migration Strategy

### Step 1: Fix Immediate Issues (Quick Wins)
1. ✅ Add `lineSpacing = 8` to device TOC columns
2. ✅ Add `preferredWidth = 200` to device TOC columns  
3. ✅ Change `CreateGuideTopImage` to use 300px, flexibleWidth=0

### Step 2: Create Unified TOC (Low Risk)
1. Create `CreateUnifiedTableOfContents` with TocConfig
2. Update all callers to use new function
3. Delete old TOC functions

### Step 3: Create Unified Section Renderer (Medium Risk)
1. Create `RenderSection` with SectionStyle enum
2. Migrate one page type at a time
3. Test thoroughly between migrations
4. Delete old section functions

### Step 4: Create Unified Page Renderer (Higher Risk)
1. Create `RenderCustomContent` entry point
2. Migrate one page type at a time
3. Ensure backward compatibility with existing JSON schemas

---

## Files That Would Be Modified

| File | Changes |
|------|---------|
| `Patches.cs` | Major refactor - reduce from ~3200 lines to ~2000 lines |
| `Models.cs` | Add `TocConfig` class |
| `JsonGuideLoader.cs` | Update to use unified data structures |
| `SurvivalManualLoader.cs` | Update to use unified data structures |
| `DeviceDatabase.cs` | Minor updates for consistency |

---

## Risk Assessment

### Low Risk Changes (Safe to Implement)

| Change | Risk Level | Why Safe | Time |
|--------|------------|----------|------|
| ✅ Image sizing fix | **Done** | Single parameter change, no logic change | 2 min |
| ✅ Device TOC spacing | **Done** | Adding parameters that already work elsewhere | 5 min |
| TOC Unification | **Low** | All 3 implementations produce identical DOM structure, just with different parameters | 15-30 min |

### Medium Risk Changes (Careful Implementation)

| Change | Risk Level | Concerns | Mitigation |
|--------|------------|----------|------------|
| Section Renderer Unification | **Medium** | Different recursive call patterns, parent type differences | Keep both implementations briefly, A/B test |

**Detailed Analysis of Section Renderer Differences:**

1. **Parent Type Mismatch:**
   - `CreateNestedCollapsibleCategory` takes `StationpediaCategory parentCategory`
   - `CreateGuideCollapsibleSection` takes `RectTransform parent`
   - `CreateSurvivalManualSection` takes `RectTransform parent`
   
   *Fix: Use `RectTransform` uniformly - extract from `StationpediaCategory.Contents`*

2. **Recursive Call Target:**
   - Devices: calls back to `RenderOperationalDetailElement`
   - Guides: calls back to `RenderGuideSectionElement`  
   - Manual: calls back to `CreateSurvivalManualSection`
   
   *Fix: Single unified function with callback or enum dispatch*

3. **Depth Handling:**
   - Devices: passes depth consistently
   - Manual: hardcodes `VanillaModeManager.GetTitleColor(1)` for sections
   
   *Fix: Always pass and use depth parameter*

4. **Missing Features:**
   - Manual section: Missing `imageFile`, `youtubeUrl`, `videoFile` support
   - Guide section: Has all features
   
   *Fix: Unified function includes all features*

### Function-by-Function Comparison

```
Feature                    | Device | Guide | Manual
---------------------------|--------|-------|--------
imageFile support          |   ✓    |   ✓   |   ✗
description support        |   ✓    |   ✓   |   ✓
items (bullets) support    |   ✓    |   ✓   |   ✓
steps (numbered) support   |   ✓    |   ✓   |   ✓
youtubeUrl support         |   ✓    |   ✓   |   ✗
videoFile support          |   ✓    |   ✓   |   ✗
children recursion         |   ✓    |   ✓   |   ✓
TOC registration           |   ✓    |   ✓   |   ✓
Depth-based coloring       |   ✓    |   ✓   |   ✗ (hardcoded)
```

**Verdict:** All three functions are 90%+ identical. The Manual section is just a subset with a hardcoded depth. Safe to unify.

---

## Recommended Unification Approach

### Step 1: Create Unified Section Renderer (Low-Medium Risk)

```csharp
private static void CreateCollapsibleSection(
    RectTransform parent,                    // Unified parent type
    TMPro.TextMeshProUGUI sourceText,
    OperationalDetail detail,
    StationpediaCategory categoryPrefab,
    UniversalPage page,
    int depth,
    string parentTocId,
    Action<RectTransform, OperationalDetail, int, string> recurseCallback)  // Handles different recursion
{
    // Single implementation with all features
}
```

**Call Sites Would Change:**
- `CreateNestedCollapsibleCategory` → `CreateCollapsibleSection(parentCategory.Contents, ...)`
- `CreateGuideCollapsibleSection` → `CreateCollapsibleSection(parent, ...)`
- `CreateSurvivalManualSection` → `CreateCollapsibleSection(parent, ...)`

### Step 2: Create Unified TOC (Low Risk)

```csharp
private static void CreateUnifiedTOC(
    RectTransform parent,
    TMPro.TextMeshProUGUI sourceText,
    List<(string tocId, string title, int depth)> entries,
    int columnSpacing = 24,
    float columnWidth = 200f,
    float lineSpacing = 8f,
    string title = "Contents")
{
    // Single implementation
}
```

All three current TOC functions become one-liner wrappers calling this with different defaults.

---

## Honest Time Estimates

| Task | Estimated Time | Risk |
|------|----------------|------|
| TOC Unification | 15-30 minutes | Low |
| Section Renderer Unification | 30-45 minutes | Medium |
| Remove old functions & test | 15 minutes | Low |
| **Total** | **1-1.5 hours** | **Medium overall** |

---

## What Could Go Wrong

1. **Visual Regression:** Unified function renders slightly differently
   - *Mitigation:* Test all page types after each change

2. **Missing Edge Case:** Some page type has a special behavior we missed
   - *Mitigation:* Grep for all usages, verify parameters

3. **Recursion Bug:** Unified callback pattern breaks nested content
   - *Mitigation:* Test deeply nested content (3+ levels)

4. **Type Errors:** Parent type mismatches
   - *Mitigation:* Compiler will catch these immediately

---

## Recommended Priority

1. ✅ **DONE**: Fix image sizing (300px, no stretch)
2. ✅ **DONE**: Fix device TOC spacing (lineSpacing=8, preferredWidth=200)
3. ✅ **DONE**: Unify TOC functions - Created `CreateUnifiedTableOfContents`
4. ✅ **DONE**: Unify section rendering - Created `CreateUnifiedCollapsibleSection` and `CreateUnifiedInlineContent`
5. **OPTIONAL**: Remove deprecated old functions (marked but not deleted)

---

## Unified Functions Created

### `CreateUnifiedTableOfContents`
Based on Survival Manual styling:
- Centered columns with `childAlignment = UpperCenter`
- 3 columns max, 8 rows per column
- 30px spacing between columns
- 200px fixed column width
- 8px line spacing to prevent overlap
- Word wrapping and ellipsis overflow
- Semi-transparent background panel

### `CreateUnifiedCollapsibleSection`
Handles all collapsible section types:
- Takes `Action<RectTransform, OperationalDetail, int, string>` callback for recursion
- Supports all content types: imageFile, description, items, steps, youtubeUrl, videoFile
- Proper TOC registration with parent hierarchy
- Depth-based title coloring via VanillaModeManager

### `CreateUnifiedInlineContent`
For non-collapsible content:
- Same feature support as collapsible sections
- Inline header styling with title color
- Proper container layout

---

## Conclusion

The current codebase has **~1200 lines of duplicated rendering logic** spread across 3 parallel implementations. Unifying this code would:

1. **Reduce maintenance burden** - Fix bugs once, not three times
2. **Ensure consistency** - Same spacing, sizing, and behavior everywhere
3. **Simplify future features** - Add new functionality in one place
4. **Reduce file size** - From ~3200 to ~2000 lines in Patches.cs

The immediate fixes (image sizing, TOC spacing) can be done in minutes. The full unification would take 1-2 days of focused work but would significantly improve long-term maintainability.
