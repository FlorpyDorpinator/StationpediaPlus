## Plan: Guide Format Additions - Advanced Wiki-Like Features

This plan implements nested collapsible dropdowns, custom icons with animation, table of contents with scroll-to navigation, and inline image support for the Stationpedia Ascended mod's Operational Details section.

**Phases (6 phases)**

1. **Phase 1: Enhanced Data Models**
    - **Objective:** Extend the JSON schema and C# models to support new features (nested dropdowns, TOC, images)
    - **Files/Functions to Modify/Create:**
      - [Models.cs](../mod/src/Data/Models.cs) - Add `collapsible`, `tocEntry`, `imageFile` properties to OperationalDetail
      - [descriptions.schema.json](../mod/descriptions.schema.json) - Update schema with new properties
    - **Tests to Write:**
      - Verify JSON parsing with new properties doesn't break existing entries
      - Test that missing optional properties default correctly
    - **Steps:**
      1. Write test JSON entry with all new properties
      2. Add new properties to OperationalDetail class
      3. Update JSON schema
      4. Build and verify parsing works

2. **Phase 2: Custom Icon System**
    - **Objective:** Load custom sprites for collapse/expand icons and support animated icon transitions
    - **Files/Functions to Modify/Create:**
      - [StationpediaAscendedMod.cs](../mod/StationpediaAscendedMod.cs) - Add icon loading for multiple frames
      - Create new `IconAnimator.cs` component for frame animation
    - **Tests to Write:**
      - Test icon loading with missing files (graceful fallback)
      - Test animation frame cycling
    - **Steps:**
      1. Create IconAnimator component class
      2. Load icon_open.png and icon_closed.png sprites
      3. Implement frame animation coroutine
      4. Test icon display on category toggle

3. **Phase 3: Nested Collapsible Dropdowns**
    - **Objective:** Transform flat OperationalDetails into hierarchical collapsible sections
    - **Files/Functions to Modify/Create:**
      - [Patches.cs](../mod/src/Harmony/Patches.cs) - Refactor CreateOperationalDetailsCategory to support nesting
      - New helper method `CreateNestedCategory()`
    - **Tests to Write:**
      - Test 3-level deep nesting renders correctly
      - Test expand/collapse propagates to children
    - **Steps:**
      1. Refactor RenderOperationalDetail to create StationpediaCategory instances for collapsible items
      2. Implement proper parent-child hierarchy using Contents transform
      3. Apply custom icons to nested categories
      4. Configure VerticalLayoutGroup and ContentSizeFitter for auto-sizing
      5. Test nested expansion and collapse

4. **Phase 4: Table of Contents Component**
    - **Objective:** Create a TOC panel that appears at top of Operational Details with clickable links
    - **Files/Functions to Modify/Create:**
      - [Patches.cs](../mod/src/Harmony/Patches.cs) - Add CreateTableOfContents() method
      - New `ScrollToCategory.cs` helper for navigation
    - **Tests to Write:**
      - Test TOC renders only when configured
      - Test link generation matches section titles
    - **Steps:**
      1. Create TOC container GameObject with styled background
      2. Generate TMP link tags for each tocEntry section
      3. Add HelpLinkHandler for click detection
      4. Store category references by ID for scroll targeting

5. **Phase 5: Scroll-To Navigation**
    - **Objective:** Clicking TOC link scrolls to that section and expands it
    - **Files/Functions to Modify/Create:**
      - [Patches.cs](../mod/src/Harmony/Patches.cs) - Implement ScrollToSection() coroutine
      - Custom link handler for TOC clicks
    - **Tests to Write:**
      - Test scrolling to first/middle/last sections
      - Test auto-expand of collapsed section
    - **Steps:**
      1. Register custom link pattern handler (toc_SectionId)
      2. Implement coroutine with proper layout timing (2 frames + ForceUpdateCanvases)
      3. Calculate normalized scroll position from RectTransform
      4. Expand target category if collapsed
      5. Smooth scroll animation (optional)

6. **Phase 6: Inline Image Support**
    - **Objective:** Allow PNG images to be inserted into Operational Details content
    - **Files/Functions to Modify/Create:**
      - [Patches.cs](../mod/src/Harmony/Patches.cs) - Add CreateInlineImage() method
      - Image loading helper similar to existing phoenix icon loading
    - **Tests to Write:**
      - Test image loads with correct aspect ratio
      - Test missing image shows placeholder
    - **Steps:**
      1. Create image loading utility for runtime PNG loading
      2. When `imageFile` property present, create Image GameObject
      3. Insert into VerticalLayoutGroup at correct position
      4. Handle aspect ratio preservation with LayoutElement
      5. Test with sample image in descriptions.json

**Open Questions**
1. Should TOC be always visible or collapsible itself? Recommend: Fixed visible panel at top
2. What size should inline images default to? Recommend: 200px wide, auto height
3. Should clicking a section in TOC also close other sections? Recommend: No, keep independent
4. Phoenix icon animation frames - how many frames and what duration? Recommend: 2 frames (open/closed), 100ms transition
5. Custom background color - configurable per-entry or global? Recommend: Global default with per-entry override option
