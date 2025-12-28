import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'r', encoding='utf-8') as f:
    desc_data = json.load(f)

# Create device key to index map
device_index = {}
for i, device in enumerate(desc_data['devices']):
    device_index[device['deviceKey']] = i

# New devices to add with full context-sensitive descriptions
new_devices = [
    {
        "deviceKey": "ThingItemMiningDrillPneumatic",
        "displayName": "Pneumatic Mining Drill",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device has air pressure to operate."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Drill mode: 0=Default (mine ore), 1=Flatten (level terrain)."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if drill has an error (e.g., no air pressure)."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when drill is actively mining."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Drill on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Default": {"modeValue": "Default", "description": "Normal mining mode - removes terrain and collects ore. Uses compressed air instead of battery."},
            "Flatten": {"modeValue": "Flatten", "description": "Flattening mode - levels terrain for construction without collecting ore. Uses compressed air."}
        }
    },
    {
        "deviceKey": "ThingItemGasCanisterSmart",
        "displayName": "Gas Canister (Smart)",
        "logicDescriptions": {
            "Pressure": {"dataType": "Float", "range": "0+ kPa", "description": "Internal gas pressure in kPa."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Internal gas temperature in Kelvin."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Fill (accept gas), 1=Empty (release gas)."},
            "RatioOxygen": {"dataType": "Float", "range": "0-1", "description": "Oxygen ratio in canister."},
            "RatioCarbonDioxide": {"dataType": "Float", "range": "0-1", "description": "CO2 ratio in canister."},
            "RatioNitrogen": {"dataType": "Float", "range": "0-1", "description": "Nitrogen ratio in canister."},
            "RatioVolatiles": {"dataType": "Float", "range": "0-1", "description": "Volatiles ratio in canister."},
            "TotalMoles": {"dataType": "Float", "range": "0+", "description": "Total gas moles in canister."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Fill mode - canister accepts gas from connected pipe network when pressure differential allows."},
            "Mode1": {"modeValue": "Mode1", "description": "Empty mode - canister releases gas into connected pipe network when pressure differential allows."}
        }
    },
    {
        "deviceKey": "ThingItemLiquidCanisterSmart",
        "displayName": "Liquid Canister (Smart)",
        "logicDescriptions": {
            "Pressure": {"dataType": "Float", "range": "0+ kPa", "description": "Internal liquid pressure in kPa."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Internal liquid temperature in Kelvin."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Fill (accept liquid), 1=Empty (release liquid)."},
            "TotalMoles": {"dataType": "Float", "range": "0+", "description": "Total liquid moles in canister."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Fill mode - canister accepts liquid from connected pipe network."},
            "Mode1": {"modeValue": "Mode1", "description": "Empty mode - canister releases liquid into connected pipe network."}
        }
    },
    {
        "deviceKey": "ThingDynamicGasTankAdvanced",
        "displayName": "Gas Tank Mk II",
        "logicDescriptions": {
            "Pressure": {"dataType": "Float", "range": "0+ kPa", "description": "Internal gas pressure in kPa."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Internal gas temperature in Kelvin."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Fill (accept gas), 1=Empty (release gas)."},
            "TotalMoles": {"dataType": "Float", "range": "0+", "description": "Total gas moles in tank."},
            "RatioOxygen": {"dataType": "Float", "range": "0-1", "description": "Oxygen ratio in tank."},
            "RatioCarbonDioxide": {"dataType": "Float", "range": "0-1", "description": "CO2 ratio in tank."},
            "RatioNitrogen": {"dataType": "Float", "range": "0-1", "description": "Nitrogen ratio in tank."},
            "RatioVolatiles": {"dataType": "Float", "range": "0-1", "description": "Volatiles ratio in tank."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Fill mode - tank accepts gas from connected pipe network."},
            "Mode1": {"modeValue": "Mode1", "description": "Empty mode - tank releases gas into connected pipe network."}
        }
    },
    {
        "deviceKey": "ThingDynamicAirConditioner",
        "displayName": "Portable Air Conditioner",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device has battery power."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Cold (cooling), 1=Hot (heating)."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Current ambient temperature reading."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Cold": {"modeValue": "Cold", "description": "Cooling mode - removes heat from surrounding atmosphere, lowering temperature. Battery-powered portable unit."},
            "Hot": {"modeValue": "Hot", "description": "Heating mode - adds heat to surrounding atmosphere, raising temperature. Battery-powered portable unit."}
        }
    },
    {
        "deviceKey": "ThingAppliancePlantGeneticStabilizer",
        "displayName": "Plant Genetic Stabilizer",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Stabilize (lock genetics), 1=Destabilize (unlock for mutation)."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Triggers genetic operation on target plant."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Stabilize": {"modeValue": "Stabilize", "description": "Stabilize mode - locks plant genetics, preventing random mutation during harvest. Use to preserve desired traits."},
            "Destabilize": {"modeValue": "Destabilize", "description": "Destabilize mode - unlocks plant genetics, enabling mutation and cross-breeding. Use for genetic experimentation."}
        }
    },
    {
        "deviceKey": "ThingCircuitboardHashDisplay",
        "displayName": "Hash Display",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Prefab (item type hash), 1=GasLiquid (contents type hash)."},
            "Setting": {"dataType": "Hash", "range": "Any", "description": "The hash value being displayed."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Prefab": {"modeValue": "Prefab", "description": "Displays PrefabHash - the item/structure type identifier. Useful for sorting systems."},
            "GasLiquid": {"modeValue": "GasLiquid", "description": "Displays the gas or liquid type hash of the contents. Useful for atmosphere monitoring."}
        }
    },
    {
        "deviceKey": "ThingLandingpad_CenterPiece01",
        "displayName": "Landingpad Center",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-4", "description": "Landing status: 0=None, 1=NoContact, 2=Moving, 3=Holding, 4=Landed."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "None": {"modeValue": "None", "description": "No landing pad status - pad is inactive or not configured."},
            "NoContact": {"modeValue": "NoContact", "description": "No spacecraft contact - pad is ready but no ship is detected."},
            "Moving": {"modeValue": "Moving", "description": "Spacecraft is moving - ship is approaching, departing, or repositioning."},
            "Holding": {"modeValue": "Holding", "description": "Spacecraft is holding position - ship is hovering above pad awaiting landing clearance."},
            "Landed": {"modeValue": "Landed", "description": "Spacecraft has landed - ship is secured on pad, ready for cargo/fuel transfer."}
        }
    },
    {
        "deviceKey": "ThingItemMiningCharge",
        "displayName": "Mining Charge",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Impact detonation, 1=Remote detonation."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Impact detonation - charge explodes on collision with terrain or objects."},
            "Mode1": {"modeValue": "Mode1", "description": "Remote detonation - charge arms on placement and waits for remote trigger signal."}
        }
    },
    {
        "deviceKey": "ThingStructureLogicTransmitter",
        "displayName": "Logic Transmitter",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Passive (receive only), 1=Active (transmit)."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "Setting": {"dataType": "Float", "range": "Any", "description": "Value being transmitted/received."},
            "Channel": {"dataType": "Integer", "range": "0+", "description": "Wireless channel for communication."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Passive": {"modeValue": "Passive", "description": "Passive mode - device only receives signals on its channel, does not broadcast."},
            "Active": {"modeValue": "Active", "description": "Active mode - device broadcasts its Setting value on the configured channel."}
        }
    },
    {
        "deviceKey": "ThingLandingpad_LiquidTankConnectorPiece",
        "displayName": "Landingpad Tank Connector (Liquid)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Refuel spacecraft, 1=Drain from spacecraft."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device has an error."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Refuel mode - transfers liquid TO the landed spacecraft from your pipe network."},
            "Mode1": {"modeValue": "Mode1", "description": "Drain mode - transfers liquid FROM the landed spacecraft into your pipe network."}
        }
    },
    {
        "deviceKey": "ThingPortableComposter",
        "displayName": "Portable Composter",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Operating mode for composting process."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when actively composting."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard composting mode - processes organic matter into fertilizer."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative composting mode - may affect output ratio or speed."}
        }
    },
    {
        "deviceKey": "ThingStructureAdvancedComposter",
        "displayName": "Advanced Composter",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Open": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if composter is open."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Operating mode for composting process."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device has an error."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when actively composting."},
            "Lock": {"dataType": "Boolean", "range": "0-1", "description": "Locks settings when set to 1."},
            "Setting": {"dataType": "Float", "range": "0+", "description": "Target setting for composting."},
            "Maximum": {"dataType": "Float", "range": "0+", "description": "Maximum capacity."},
            "Ratio": {"dataType": "Float", "range": "0-1", "description": "Current fill ratio."},
            "Quantity": {"dataType": "Integer", "range": "0+", "description": "Number of items being processed."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Device on/off state."},
            "ExportCount": {"dataType": "Integer", "range": "0+", "description": "Total items exported."},
            "ImportCount": {"dataType": "Integer", "range": "0+", "description": "Total items imported."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard composting - processes organic matter into fertilizer at normal rate."},
            "Mode1": {"modeValue": "Mode1", "description": "Enhanced composting - may use more power but processes faster or produces better output."}
        }
    },
    {
        "deviceKey": "ThingStructureAdvancedFurnace",
        "displayName": "Advanced Furnace",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device is powered."},
            "Open": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if furnace door is open."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Smelt (combine ores), 1=Refine (purify single ore type)."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if furnace has an error."},
            "Pressure": {"dataType": "Float", "range": "0+ kPa", "description": "Internal atmosphere pressure."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Internal temperature in Kelvin."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when actively smelting."},
            "Reagents": {"dataType": "Integer", "range": "0+", "description": "Number of reagent items loaded."},
            "RecipeHash": {"dataType": "Hash", "range": "CRC32", "description": "Hash of current recipe being processed."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Furnace on/off state."},
            "SettingInput": {"dataType": "Float", "range": "0+", "description": "Input pressure setting."},
            "SettingOutput": {"dataType": "Float", "range": "0+", "description": "Output pressure setting."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Smelting mode - combines multiple ore types into alloys according to recipes."},
            "Mode1": {"modeValue": "Mode1", "description": "Refining mode - purifies single ore type, removing impurities for higher quality output."}
        }
    },
    {
        "deviceKey": "ThingStructureFurnace",
        "displayName": "Furnace",
        "logicDescriptions": {
            "Open": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if furnace door is open."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "0=Smelt (combine ores), 1=Refine (purify single ore type)."},
            "Pressure": {"dataType": "Float", "range": "0+ kPa", "description": "Internal atmosphere pressure."},
            "Temperature": {"dataType": "Float", "range": "0+ K", "description": "Internal temperature in Kelvin."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when actively smelting."},
            "Reagents": {"dataType": "Integer", "range": "0+", "description": "Number of reagent items loaded."},
            "RecipeHash": {"dataType": "Hash", "range": "CRC32", "description": "Hash of current recipe being processed."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Smelting mode - combines multiple ore types into alloys. Requires correct ratios and temperature."},
            "Mode1": {"modeValue": "Mode1", "description": "Refining mode - purifies single ore type into pure ingots. Higher temperature yields better purity."}
        }
    },
    {
        "deviceKey": "ThingStructureCamera",
        "displayName": "Camera",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera display mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard view mode - normal camera feed."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative view mode - may provide different visual filter or overlay."}
        }
    },
    {
        "deviceKey": "ThingStructureSecurityCameraFishEye",
        "displayName": "CCTV Camera (Fish-Eye)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if camera is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera display mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard fish-eye view - wide-angle surveillance with full room coverage."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative mode - may adjust distortion correction or field of view."}
        }
    },
    {
        "deviceKey": "ThingStructureSecurityCameraLeft",
        "displayName": "CCTV Camera (Left)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if camera is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera display mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard left-angle view - fixed surveillance angled left."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative mode - may provide different visual processing."}
        }
    },
    {
        "deviceKey": "ThingStructureSecurityCameraPanning",
        "displayName": "CCTV Camera (Panning)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if camera is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera panning mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Auto-pan mode - camera automatically sweeps back and forth for wider coverage."},
            "Mode1": {"modeValue": "Mode1", "description": "Manual/Fixed mode - camera stays at set position for focused surveillance."}
        }
    },
    {
        "deviceKey": "ThingStructureSecurityCameraRight",
        "displayName": "CCTV Camera (Right)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if camera is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera display mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard right-angle view - fixed surveillance angled right."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative mode - may provide different visual processing."}
        }
    },
    {
        "deviceKey": "ThingStructureSecurityCameraStraight",
        "displayName": "CCTV Camera (Straight)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if camera is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Camera display mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Camera on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard forward view - fixed surveillance pointing straight ahead."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternative mode - may provide different visual processing."}
        }
    },
    {
        "deviceKey": "ThingStructureSpotlight",
        "displayName": "Spotlight",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if spotlight is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Spotlight tracking mode."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if device has an error."},
            "Horizontal": {"dataType": "Float", "range": "-180 to 180", "description": "Horizontal rotation angle in degrees."},
            "Vertical": {"dataType": "Float", "range": "-90 to 90", "description": "Vertical tilt angle in degrees."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Spotlight on/off state."},
            "PositionX": {"dataType": "Float", "range": "Any", "description": "X world position of spotlight."},
            "PositionY": {"dataType": "Float", "range": "Any", "description": "Y world position of spotlight."},
            "PositionZ": {"dataType": "Float", "range": "Any", "description": "Z world position of spotlight."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Manual mode - spotlight angle controlled by Horizontal/Vertical logic values."},
            "Mode1": {"modeValue": "Mode1", "description": "Tracking mode - spotlight may automatically track movement or targets."}
        }
    },
    {
        "deviceKey": "ThingStructureGlowLight2",
        "displayName": "Flood Light (Large)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if light is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Light operation mode."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Activation trigger."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Light on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Normal mode - constant illumination when On."},
            "Mode1": {"modeValue": "Mode1", "description": "Logic mode - responds to Activate signal for automated lighting control."}
        }
    },
    {
        "deviceKey": "ThingStructureHorizontalAutoMiner",
        "displayName": "OGRE (Horizontal Auto Miner)",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if OGRE is powered."},
            "Open": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if output hatch is open."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Mining operation mode."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if OGRE has an error (e.g., full storage, no terrain)."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 when actively mining."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "OGRE on/off state."},
            "ExportCount": {"dataType": "Integer", "range": "0+", "description": "Total ore stacks exported."},
            "ImportCount": {"dataType": "Integer", "range": "0+", "description": "Total items imported (if any)."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard mining - OGRE mines forward horizontally, collecting all ore types."},
            "Mode1": {"modeValue": "Mode1", "description": "Selective mining - OGRE may filter ore types or adjust mining behavior."}
        }
    },
    {
        "deviceKey": "ThingStructureSDBSilo",
        "displayName": "SDB Silo",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if silo is powered."},
            "Open": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if silo doors are open."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Silo operation mode."},
            "Error": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if silo has an error."},
            "Activate": {"dataType": "Boolean", "range": "0-1", "description": "Triggers launch when set to 1."},
            "Lock": {"dataType": "Boolean", "range": "0-1", "description": "Locks silo when set to 1."},
            "Quantity": {"dataType": "Integer", "range": "0+", "description": "Number of SDBs loaded in silo."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Silo on/off state."},
            "ExportCount": {"dataType": "Integer", "range": "0+", "description": "Total SDBs launched."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Armed mode - SDB silo ready to launch on Activate signal."},
            "Mode1": {"modeValue": "Mode1", "description": "Safe mode - SDB silo will not launch, safe for loading/maintenance."}
        }
    },
    {
        "deviceKey": "ThingModularDeviceLabelDiode2",
        "displayName": "Label Diode 2",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if diode is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Display mode for the label."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Diode on/off state."},
            "Color": {"dataType": "Integer", "range": "0+", "description": "RGB color value for display."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard display - shows label with configured color."},
            "Mode1": {"modeValue": "Mode1", "description": "Blinking mode - label blinks for attention/alerts."}
        }
    },
    {
        "deviceKey": "ThingModularDeviceLabelDiode3",
        "displayName": "Label Diode 3",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if diode is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Display mode for the label."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Diode on/off state."},
            "Color": {"dataType": "Integer", "range": "0+", "description": "RGB color value for display."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Standard display - shows label with configured color."},
            "Mode1": {"modeValue": "Mode1", "description": "Blinking mode - label blinks for attention/alerts."}
        }
    },
    {
        "deviceKey": "ThingModularDeviceNumpad",
        "displayName": "Logic Num Pad",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if numpad is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Numpad input mode."},
            "Setting": {"dataType": "Float", "range": "Any", "description": "Current entered value."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Numpad on/off state."},
            "Color": {"dataType": "Integer", "range": "0+", "description": "RGB color value for display."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Input mode - numpad accepts digit input, Setting updates on confirm."},
            "Mode1": {"modeValue": "Mode1", "description": "Display mode - numpad shows Setting value without accepting input."}
        }
    },
    {
        "deviceKey": "ThingStructureChuteFlipFlopSplitter",
        "displayName": "Chute Flip Flop Splitter",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Splitter direction mode."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Output left - next item routes to left output."},
            "Mode1": {"modeValue": "Mode1", "description": "Output right - next item routes to right output."}
        }
    },
    {
        "deviceKey": "ThingStructureChuteDigitalFlipFlopSplitterLeft",
        "displayName": "Chute Digital Flip Flop Splitter Left",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if splitter is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Routing mode for item flow."},
            "Setting": {"dataType": "Integer", "range": "0+", "description": "Count setting for batch routing."},
            "Quantity": {"dataType": "Integer", "range": "0+", "description": "Current count in batch."},
            "SettingOutput": {"dataType": "Integer", "range": "0+", "description": "Output batch size setting."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Splitter on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Count mode - routes Setting number of items left, then switches."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternate mode - alternates each item between outputs."}
        }
    },
    {
        "deviceKey": "ThingStructureChuteDigitalFlipFlopSplitterRight",
        "displayName": "Chute Digital Flip Flop Splitter Right",
        "logicDescriptions": {
            "Power": {"dataType": "Boolean", "range": "0-1", "description": "Returns 1 if splitter is powered."},
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Routing mode for item flow."},
            "Setting": {"dataType": "Integer", "range": "0+", "description": "Count setting for batch routing."},
            "Quantity": {"dataType": "Integer", "range": "0+", "description": "Current count in batch."},
            "SettingOutput": {"dataType": "Integer", "range": "0+", "description": "Output batch size setting."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Splitter on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Count mode - routes Setting number of items right, then switches."},
            "Mode1": {"modeValue": "Mode1", "description": "Alternate mode - alternates each item between outputs."}
        }
    },
    {
        "deviceKey": "ThingStructurePipeOrgan",
        "displayName": "Pipe Organ",
        "logicDescriptions": {
            "Mode": {"dataType": "Integer", "range": "0-1", "description": "Organ playback mode."},
            "On": {"dataType": "Boolean", "range": "0-1", "description": "Organ on/off state."},
            "ReferenceId": {"dataType": "Hash", "range": "Unique", "description": "Unique reference ID."}
        },
        "modeDescriptions": {
            "Mode0": {"modeValue": "Mode0", "description": "Manual mode - plays notes based on logic input signals."},
            "Mode1": {"modeValue": "Mode1", "description": "Sequence mode - plays pre-programmed musical sequence."}
        }
    }
]

# Add new devices and update existing ones
added = 0
updated = 0

for new_device in new_devices:
    key = new_device["deviceKey"]
    if key in device_index:
        # Update existing device with new data
        idx = device_index[key]
        existing = desc_data['devices'][idx]
        
        # Merge logicDescriptions
        if 'logicDescriptions' not in existing:
            existing['logicDescriptions'] = {}
        for logic_key, logic_val in new_device.get('logicDescriptions', {}).items():
            if logic_key not in existing['logicDescriptions']:
                existing['logicDescriptions'][logic_key] = logic_val
        
        # Add modeDescriptions if not present
        if 'modeDescriptions' not in existing and 'modeDescriptions' in new_device:
            existing['modeDescriptions'] = new_device['modeDescriptions']
            updated += 1
            print(f"Updated: {key}")
        
        # Update displayName if missing
        if not existing.get('displayName') and new_device.get('displayName'):
            existing['displayName'] = new_device['displayName']
    else:
        # Add new device
        desc_data['devices'].append(new_device)
        added += 1
        print(f"Added: {key}")

# Save the updated file
with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'w', encoding='utf-8') as f:
    json.dump(desc_data, f, indent=2, ensure_ascii=False)

print(f"\nDone! Added {added} new devices, updated {updated} existing devices.")
