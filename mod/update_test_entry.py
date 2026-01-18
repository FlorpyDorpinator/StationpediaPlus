"""
Update descriptions.json to add a comprehensive test entry for the new Guide Format features
"""
import json

# Read the existing file
with open(r"c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json", "r", encoding="utf-8") as f:
    data = json.load(f)

# Find and update the SolidFuelGenerator entry with new features
for i, device in enumerate(data.get("devices", [])):
    if device.get("deviceKey") == "ThingStructureSolidFuelGenerator":
        # Replace with comprehensive test entry
        data["devices"][i] = {
            "deviceKey": "ThingStructureSolidFuelGenerator",
            "displayName": "📖 GUIDE FORMAT TEST - Solid Fuel Generator",
            "pageDescriptionPrepend": "<color=#00FF00><b>🧪 GUIDE FORMAT ADDITIONS TEST PAGE</b></color>\nThis page demonstrates ALL new features including nested collapsibles, TOC, and images.\n\n",
            "generateToc": True,
            "tocTitle": "📚 Quick Navigation",
            "operationalDetailsTitleColor": "#FF7A18",
            "operationalDetailsBackgroundColor": "#0A1520",
            "OperationalDetails": [
                {
                    "title": "Basic Overview",
                    "tocId": "overview",
                    "collapsible": True,
                    "description": "This is a COLLAPSIBLE section! Click the icon to expand/collapse.\n\nThe Solid Fuel Generator burns coal, biomass, or other solid fuels to produce electricity.",
                    "items": [
                        "Input: Coal, Charcoal, or Biomass",
                        "Output: Up to 5kW of power",
                        "Requires: Oxygen atmosphere"
                    ]
                },
                {
                    "title": "Fuel Types",
                    "tocId": "fuels",
                    "collapsible": True,
                    "description": "Different fuels have different burn rates and efficiency:",
                    "children": [
                        {
                            "title": "Coal",
                            "tocId": "coal",
                            "collapsible": True,
                            "description": "Primary fuel source, obtained from mining coal ore.",
                            "items": [
                                "Burn time: ~180 seconds",
                                "Power output: 5kW",
                                "Pollution: High"
                            ]
                        },
                        {
                            "title": "Charcoal",
                            "tocId": "charcoal",
                            "collapsible": True,
                            "description": "Made by processing wood in a furnace.",
                            "items": [
                                "Burn time: ~120 seconds",
                                "Power output: 4kW",
                                "Pollution: Medium"
                            ]
                        },
                        {
                            "title": "Biomass",
                            "tocId": "biomass",
                            "collapsible": True,
                            "description": "Renewable fuel from plants.",
                            "items": [
                                "Burn time: ~60 seconds",
                                "Power output: 2kW",
                                "Pollution: Low"
                            ]
                        }
                    ]
                },
                {
                    "title": "Setup Guide",
                    "tocId": "setup",
                    "collapsible": True,
                    "description": "Follow these steps to set up your generator:",
                    "steps": [
                        "Place the Solid Fuel Generator on a solid surface",
                        "Connect power cables to the generator",
                        "Ensure the room has oxygen (required for combustion)",
                        "Insert fuel into the generator slot",
                        "Set the generator to ON using logic or manual switch"
                    ]
                },
                {
                    "title": "Inline Text Section (Non-Collapsible)",
                    "description": "This section is NOT collapsible because 'collapsible' is not set to true. It appears as inline text with TMP formatting support.\n\n<color=#FFAA00>Note:</color> Use collapsible sections for longer content that users may want to hide."
                },
                {
                    "title": "Troubleshooting",
                    "tocId": "troubleshooting",
                    "collapsible": True,
                    "description": "Common issues and solutions:",
                    "children": [
                        {
                            "title": "Generator Won't Start",
                            "collapsible": True,
                            "items": [
                                "Check if fuel is loaded",
                                "Verify oxygen level in room (needs O2 for combustion)",
                                "Ensure 'On' state is set to 1"
                            ]
                        },
                        {
                            "title": "Low Power Output",
                            "collapsible": True,
                            "items": [
                                "Check fuel type (coal > charcoal > biomass)",
                                "Verify cable connections",
                                "Check for power network overload"
                            ]
                        }
                    ]
                }
            ]
        }
        print(f"Updated entry at index {i}")
        break

# Write back
with open(r"c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaAscended\mod\descriptions.json", "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)

print("Done! descriptions.json updated with new test entry.")
