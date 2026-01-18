# 🔥 Stationpedia Ascended

**An enhanced Stationpedia experience for Stationeers with comprehensive tooltips, bug fixes, and quality of life improvements.**

![Stationpedia Ascended Header](assets/header-screenshot.png)
*The new Stationpedia Ascended interface with custom phoenix branding*

## ✨ Features


### 📑 Multi-Column Table of Contents & Massive additions to the nested schema/formatting
For guides with many sections, the TOC now automatically arranges into columns:
- Maximum 8 entries per column
- Automatically expands horizontally for large guides
- Clickable links scroll to each sectio
- Tons of new formatting options for adding guides and other things

### 🎯 Enhanced Tooltips System
Get detailed explanations for every aspect of your devices with our comprehensive tooltip system:

- **Logic Descriptions** - Hover over logic types to understand their functionality
- **Slot Information** - Detailed explanations of what each slot type does
- **Memory & Registers** - Clear descriptions of memory addresses and their purposes  
- **Device Modes** - Understand different operational modes for devices
- **Connection Types** - Learn about different connection types and their uses

![Tooltips Demo](assets/tooltips-demo.gif)
*Comprehensive tooltips provide detailed information on hover*

### 🔍 Vastly Improved Search Functionality
- **Exact Match Prioritization** - Puts the word "Corn" up first if you type it
- **Categorical Search** - Sorts by categories now
- **Removed Wreckage/Ruptured/Burnt items** - Stuff that the devs forgot to hide in Stationpedia is now hidden
![Tooltips Demo](assets/search-demo.gif)


### 🔧 Operational Details Section
A dedicated section for device-specific operational details and advanced functionality:

- Appears at the top of device pages for immediate visibility
- Phoenix icon displayed next to the category title
- Configurable title color via JSON
- Collapsible by default to maintain clean interface
- Fully customizable via JSON configuration
- Explains unique device behaviors not covered in standard descriptions

![Operational Details](assets/operational-details.gif)
*Operational Details section provides advanced device information*

### 📝 Page Description Customization
Complete control over Stationpedia page descriptions:

- **Replace** existing descriptions entirely
- **Append** additional information after existing content
- **Prepend** important notes before existing descriptions
- JSON-based configuration for easy management


### 🐛 Stationpedia Bug Fixes

#### Scrollbar Handle Fix
Resolves the frustrating scrollbar handle disappearance bug that affects the base game:
- Handles now remain visible on all Stationpedia pages
- Fixes position corruption that caused handles to vanish
- Implements robust 5-frame correction system

![Scrollbar Fix](assets/scrollbar-fix.gif)
*Before and after: Scrollbar handles now work correctly on all pages*

#### Window Dragging Fix  
Eliminates crashes when dragging the Stationpedia window in the main menu:
- No more null reference exceptions
- Smooth dragging experience in all game states
- Maintains window bounds and positioning

### 🎨 Custom Branding
- **Phoenix Logo** - Custom phoenix icon replaces the original book icon
- **"Stationpedia Ascended"** branding in the window header
- **Orange Accent Colors** - Consistent theming throughout the interface
- Maintains compatibility with existing UI elements

![Custom Branding](assets/custom-branding.gif)
*Custom phoenix logo and Stationpedia Ascended branding*

## Installation

### Method 1: Via StationeersLaunchPad (Recommended)

1. Install [StationeersLaunchPad](https://github.com/StationeersLaunchPad/StationeersLaunchPad/releases) (follow the instructions on that GitHub to install Bepinex etc.)
2. Go to Steam Workshop and subscribe to Stationpedia Ascended [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3634225688)
3. Start the game and make sure you see the StationeersLaunchPad black window during load
4. If you hit f1 you should see Stationpedia Ascended & our logo at the top of your Stationpedia!


## ⚙️ Configuration

### descriptions.json Schema

The mod uses a powerful JSON configuration system for creating rich documentation.

#### Device Entry Structure
```json
{
  "deviceKey": "ThingStructureFurnace",
  "displayName": "Furnace",
  
  "generateToc": true,
  "tocTitle": "Furnace Guide",
  
  "pageDescription": "Replaces entire description",
  "pageDescriptionAppend": "Added to end",
  "pageDescriptionPrepend": "Added to beginning",
  
  "operationalDetailsTitleColor": "#FF7A18",
  "operationalDetailsBackgroundColor": "#1A2B3C",
  
  "operationalDetails": [ ... ],
  "logicDescriptions": { ... },
  "modeDescriptions": { ... },
  "slotDescriptions": { ... }
}
```

#### Operational Details Properties
Each section in `operationalDetails` supports:

| Property | Type | Description |
|----------|------|-------------|
| `title` | string | Section heading |
| `description` | string | Main text content (supports TMP rich text) |
| `items` | array | Bullet list (• prefix) |
| `steps` | array | Numbered list (1. 2. 3.) |
| `children` | array | Nested sub-sections |
| `collapsible` | boolean | Make section expandable/collapsible |
| `tocId` | string | ID for Table of Contents linking |
| `imageFile` | string | Image from mod/images/ folder |
| `videoFile` | string | Embedded MP4 video player |
| `youtubeUrl` | string | Clickable YouTube link |
| `youtubeLabel` | string | Custom YouTube link text |
| `backgroundColor` | string | Hex color for section background |

#### Example: Full Device Entry
```json
{
  "deviceKey": "ThingStructureFurnace",
  "generateToc": true,
  "tocTitle": "Furnace Guide",
  "operationalDetails": [
    {
      "title": "Overview",
      "tocId": "overview",
      "collapsible": true,
      "description": "The Furnace smelts ores into ingots.",
      "imageFile": "furnace_diagram.png"
    },
    {
      "title": "Steel Smelting",
      "tocId": "steel",
      "collapsible": true,
      "children": [
        {
          "title": "Requirements",
          "tocId": "steel-reqs",
          "collapsible": true,
          "items": [
            "Iron Ore + Coal in 3:1 ratio",
            "Temperature above 900K"
          ]
        },
        {
          "title": "Process",
          "tocId": "steel-steps",
          "collapsible": true,
          "steps": [
            "Load iron ore and coal",
            "Add fuel ice",
            "Close door and activate"
          ]
        }
      ]
    },
    {
      "title": "Video Tutorial",
      "tocId": "video",
      "collapsible": true,
      "youtubeUrl": "https://www.youtube.com/watch?v=EXAMPLE",
      "youtubeLabel": "Watch Tutorial"
    }
  ],
  "logicDescriptions": {
    "Temperature": {
      "dataType": "Float",
      "range": "0+",
      "description": "Internal temperature in Kelvin."
    }
  }
}
```

#### Generic Descriptions (Fallback)
```json
{
  "genericDescriptions": {
    "logic": {
      "Power": "Device power state (0=off, 1=on)",
      "Setting": "Configurable parameter value"
    },
    "slots": {
      "InputSlot": "Primary input for materials"
    },
    "modes": {
      "0": "Idle state",
      "1": "Active state"
    }
  }
}
```

### LLM Instructions
For AI-assisted documentation writing, see `mod/LLM_INSTRUCTIONS.txt` which contains the complete schema reference and examples.

## 🤝 Contributing

We welcome contributions! Areas where you can help:

- **Descriptions Database** - Add more comprehensive logic/slot/memory descriptions
- **Device Behaviors** - Document special behaviors for various devices
- **Bug Reports** - Report any issues or conflicts with other mods
- **Feature Requests** - Suggest new functionality or improvements

### Development Setup
1. Clone the repository
2. Install .NET Framework 4.8
3. Reference Stationeers assemblies in project
4. Use hot-reload for rapid testing

## 📋 Requirements

- **Stationeers** (Latest Stable Version)
- **StationeersLaunchPad** (Latest Version)
- **Bepinex** 


## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/FlorpyDorpinator/StationpediaAscended/issues)
- **Discord**: [Stationeers Modding Community](https://discord.com/channels/1370137389837717545/1454566815903514625r)

## 📜 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Stationeers Development Team** - For creating an amazing game with extensive modding support
- **BepInEx Team** - For the excellent modding framework
- **Stationeers Modding Community** - For inspiration, feedback, and collaboration. In particular Aproprosmath and JoeDiertay for their incrediblly generous advice, support and help making this.
- **Contributors** - Thank you to everyone who helps improve this mod


*Made with ❤️ for the Stationeers community*

