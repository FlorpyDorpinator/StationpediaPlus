import json

# Load both files
with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    sp_data = json.load(f)

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'r', encoding='utf-8') as f:
    desc_data = json.load(f)

# Create device key to index map
device_index = {}
for i, device in enumerate(desc_data['devices']):
    device_index[device['deviceKey']] = i

# Devices to skip (already have modeDescriptions)
skip_devices = {
    'ThingRobot', 'ThingStructureActiveVent', 'ThingStructureAirConditioner',
    'ThingStructureFiltration', 'ThingStructureLogicMemory', 'ThingStructureSorter'
}

# Device-specific mode descriptions
device_mode_descriptions = {
    # Mining Drills
    'ThingItemMiningDrill': {
        'Default': {'modeValue': 'Default', 'description': 'Normal mining mode - removes terrain and collects ore into your inventory.'},
        'Flatten': {'modeValue': 'Flatten', 'description': 'Flattening mode - levels terrain to create flat surfaces for building without collecting ore.'}
    },
    'ThingItemMKIIMiningDrill': {
        'Default': {'modeValue': 'Default', 'description': 'Normal mining mode - faster ore collection with deeper penetration.'},
        'Flatten': {'modeValue': 'Flatten', 'description': 'Flattening mode - quickly levels terrain for large construction projects.'}
    },
    'ThingItemMiningDrillHeavy': {
        'Default': {'modeValue': 'Default', 'description': 'Normal mining mode - maximum power for tough terrain and dense ore.'},
        'Flatten': {'modeValue': 'Flatten', 'description': 'Flattening mode - powerful leveling for industrial-scale terrain modification.'}
    },
    'ThingItemMiningDrillPneumatic': {
        'Default': {'modeValue': 'Default', 'description': 'Normal mining mode - air-powered drilling for continuous operation.'},
        'Flatten': {'modeValue': 'Flatten', 'description': 'Flattening mode - pneumatic leveling without battery drain.'}
    },
    
    # Smart Canisters
    'ThingItemGasCanisterSmart': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Fill mode - canister accepts gas from connected pipe networks.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Empty mode - canister releases gas into connected pipe networks.'}
    },
    'ThingItemLiquidCanisterSmart': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Fill mode - canister accepts liquid from connected pipe networks.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Empty mode - canister releases liquid into connected pipe networks.'}
    },
    
    # Advanced Gas Tank
    'ThingDynamicGasTankAdvanced': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Fill mode - tank accepts gas from connected pipe networks.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Empty mode - tank releases gas into connected pipe networks.'}
    },
    
    # Logic Gates
    'ThingStructureLogicGate': {
        'AND': {'modeValue': 'AND', 'description': 'Outputs 1 only if ALL inputs are 1 (true). Used for conditions requiring multiple factors.'},
        'OR': {'modeValue': 'OR', 'description': 'Outputs 1 if ANY input is 1 (true). Used for conditions where any single factor is sufficient.'},
        'NAND': {'modeValue': 'NAND', 'description': 'NOT-AND: Outputs 0 only if ALL inputs are 1. Inverse of AND operation.'},
        'NOR': {'modeValue': 'NOR', 'description': 'NOT-OR: Outputs 1 only if ALL inputs are 0. Inverse of OR operation.'},
        'XOR': {'modeValue': 'XOR', 'description': 'Exclusive OR: Outputs 1 if inputs are DIFFERENT. Used for toggle/flip-flop logic.'},
        'XNOR': {'modeValue': 'XNOR', 'description': 'Exclusive NOR: Outputs 1 if inputs are SAME. Used for equality checking.'}
    },
    
    # Logic Math
    'ThingStructureLogicMath': {
        'Add': {'modeValue': 'Add', 'description': 'Addition: Output = Input1 + Input2. Basic arithmetic sum.'},
        'Subtract': {'modeValue': 'Subtract', 'description': 'Subtraction: Output = Input1 - Input2. Basic arithmetic difference.'},
        'Multiply': {'modeValue': 'Multiply', 'description': 'Multiplication: Output = Input1 × Input2. Basic arithmetic product.'},
        'Divide': {'modeValue': 'Divide', 'description': 'Division: Output = Input1 ÷ Input2. Returns 0 if Input2 is 0.'},
        'Mod': {'modeValue': 'Mod', 'description': 'Modulo: Output = remainder of Input1 ÷ Input2. Useful for cycling values.'},
        'Pow': {'modeValue': 'Pow', 'description': 'Power: Output = Input1 raised to the power of Input2.'},
        'Log': {'modeValue': 'Log', 'description': 'Logarithm: Output = log base Input2 of Input1.'},
        'Atan2': {'modeValue': 'Atan2', 'description': 'Arctangent2: Output = angle in radians from coordinates (Input1, Input2).'}
    },
    
    # Logic Math Unary
    'ThingStructureLogicMathUnary': {
        'Abs': {'modeValue': 'Abs', 'description': 'Absolute value: Output = |Input|. Removes negative sign.'},
        'Sqrt': {'modeValue': 'Sqrt', 'description': 'Square root: Output = √Input. Returns 0 for negative inputs.'},
        'Sin': {'modeValue': 'Sin', 'description': 'Sine: Output = sin(Input). Input in radians, output -1 to 1.'},
        'Cos': {'modeValue': 'Cos', 'description': 'Cosine: Output = cos(Input). Input in radians, output -1 to 1.'},
        'Tan': {'modeValue': 'Tan', 'description': 'Tangent: Output = tan(Input). Input in radians.'},
        'Asin': {'modeValue': 'Asin', 'description': 'Arc sine: Output = asin(Input). Input -1 to 1, output in radians.'},
        'Acos': {'modeValue': 'Acos', 'description': 'Arc cosine: Output = acos(Input). Input -1 to 1, output in radians.'},
        'Atan': {'modeValue': 'Atan', 'description': 'Arc tangent: Output = atan(Input). Output in radians.'},
        'Exp': {'modeValue': 'Exp', 'description': 'Exponential: Output = e^Input. Natural exponential function.'},
        'Log': {'modeValue': 'Log', 'description': 'Natural logarithm: Output = ln(Input). Base-e logarithm.'},
        'Ceil': {'modeValue': 'Ceil', 'description': 'Ceiling: Output = smallest integer ≥ Input. Rounds up.'},
        'Floor': {'modeValue': 'Floor', 'description': 'Floor: Output = largest integer ≤ Input. Rounds down.'},
        'Round': {'modeValue': 'Round', 'description': 'Round: Output = nearest integer to Input. Standard rounding.'},
        'Rand': {'modeValue': 'Rand', 'description': 'Random: Output = random value from 0 to Input. Generates randomness.'},
        'Not': {'modeValue': 'Not', 'description': 'Logical NOT: Output = 1 if Input is 0, else 0. Inverts boolean.'}
    },
    
    # Logic Compare
    'ThingStructureLogicCompare': {
        'Equals': {'modeValue': 'Equals', 'description': 'Outputs 1 if Input1 equals Input2, else 0. Exact comparison.'},
        'NotEquals': {'modeValue': 'NotEquals', 'description': 'Outputs 1 if Input1 does NOT equal Input2, else 0. Difference check.'},
        'Greater': {'modeValue': 'Greater', 'description': 'Outputs 1 if Input1 > Input2, else 0. Greater than comparison.'},
        'Less': {'modeValue': 'Less', 'description': 'Outputs 1 if Input1 < Input2, else 0. Less than comparison.'}
    },
    
    # Logic Select
    'ThingStructureLogicSelect': {
        'Equals': {'modeValue': 'Equals', 'description': 'Selects input where value equals comparison value.'},
        'NotEquals': {'modeValue': 'NotEquals', 'description': 'Selects input where value does not equal comparison value.'},
        'Greater': {'modeValue': 'Greater', 'description': 'Selects input with greater value.'},
        'Less': {'modeValue': 'Less', 'description': 'Selects input with lesser value.'}
    },
    
    # Logic Sorter
    'ThingStructureLogicSorter': {
        'All': {'modeValue': 'All', 'description': 'ALL mode - only passes items matching ALL specified criteria.'},
        'Any': {'modeValue': 'Any', 'description': 'ANY mode - passes items matching ANY of the specified criteria.'}
    },
    
    # Klaxon (Alarm)
    'ThingStructureKlaxon': {
        'Alert': {'modeValue': 'Alert', 'description': 'General alert alarm sound.'},
        'Alarm1': {'modeValue': 'Alarm1', 'description': 'Alarm sound variant 1.'},
        'Alarm2': {'modeValue': 'Alarm2', 'description': 'Alarm sound variant 2.'},
        'Alarm3': {'modeValue': 'Alarm3', 'description': 'Alarm sound variant 3.'},
        'Alarm4': {'modeValue': 'Alarm4', 'description': 'Alarm sound variant 4.'},
        'Alarm5': {'modeValue': 'Alarm5', 'description': 'Alarm sound variant 5.'},
        'Alarm6': {'modeValue': 'Alarm6', 'description': 'Alarm sound variant 6.'},
        'Alarm7': {'modeValue': 'Alarm7', 'description': 'Alarm sound variant 7.'},
        'Alarm8': {'modeValue': 'Alarm8', 'description': 'Alarm sound variant 8.'},
        'Alarm9': {'modeValue': 'Alarm9', 'description': 'Alarm sound variant 9.'},
        'Alarm10': {'modeValue': 'Alarm10', 'description': 'Alarm sound variant 10.'},
        'Alarm11': {'modeValue': 'Alarm11', 'description': 'Alarm sound variant 11.'},
        'Alarm12': {'modeValue': 'Alarm12', 'description': 'Alarm sound variant 12.'},
        'One': {'modeValue': 'One', 'description': 'Countdown voice: "One".'},
        'Two': {'modeValue': 'Two', 'description': 'Countdown voice: "Two".'},
        'Three': {'modeValue': 'Three', 'description': 'Countdown voice: "Three".'},
        'Four': {'modeValue': 'Four', 'description': 'Countdown voice: "Four".'},
        'Five': {'modeValue': 'Five', 'description': 'Countdown voice: "Five".'},
        'Floor': {'modeValue': 'Floor', 'description': 'Floor announcement sound.'},
        'Welcome': {'modeValue': 'Welcome', 'description': 'Welcome announcement sound.'},
        'LiftOff': {'modeValue': 'LiftOff', 'description': 'Rocket liftoff announcement.'},
        'FireFireFire': {'modeValue': 'FireFireFire', 'description': 'Fire alarm: "Fire Fire Fire!"'},
        'IntruderAlert': {'modeValue': 'IntruderAlert', 'description': 'Intruder alert warning sound.'},
        'HaltWhoGoesThere': {'modeValue': 'HaltWhoGoesThere', 'description': 'Security challenge: "Halt! Who goes there?"'},
        'PressureHigh': {'modeValue': 'PressureHigh', 'description': 'High pressure warning announcement.'},
        'PressureLow': {'modeValue': 'PressureLow', 'description': 'Low pressure warning announcement.'},
        'TemperatureHigh': {'modeValue': 'TemperatureHigh', 'description': 'High temperature warning announcement.'},
        'TemperatureLow': {'modeValue': 'TemperatureLow', 'description': 'Low temperature warning announcement.'},
        'HighCarbonDioxide': {'modeValue': 'HighCarbonDioxide', 'description': 'High CO2 level warning announcement.'},
        'PollutantsDetected': {'modeValue': 'PollutantsDetected', 'description': 'Pollutant detection warning announcement.'},
        'PowerLow': {'modeValue': 'PowerLow', 'description': 'Low power warning announcement.'},
        'StormIncoming': {'modeValue': 'StormIncoming', 'description': 'Incoming storm warning announcement.'},
        'SystemFailure': {'modeValue': 'SystemFailure', 'description': 'System failure warning announcement.'},
        'MalfunctionDetected': {'modeValue': 'MalfunctionDetected', 'description': 'Malfunction detection warning announcement.'},
        'RocketLaunching': {'modeValue': 'RocketLaunching', 'description': 'Rocket launching announcement.'},
        'TraderIncoming': {'modeValue': 'TraderIncoming', 'description': 'Incoming trader announcement.'},
        'TraderLanded': {'modeValue': 'TraderLanded', 'description': 'Trader landed announcement.'},
        'Music1': {'modeValue': 'Music1', 'description': 'Background music track 1.'},
        'Music2': {'modeValue': 'Music2', 'description': 'Background music track 2.'},
        'Music3': {'modeValue': 'Music3', 'description': 'Background music track 3.'}
    },
    
    # Air Conditioner (Portable)
    'ThingDynamicAirConditioner': {
        'Cold': {'modeValue': 'Cold', 'description': 'Cooling mode - removes heat from the atmosphere, lowering temperature.'},
        'Hot': {'modeValue': 'Hot', 'description': 'Heating mode - adds heat to the atmosphere, raising temperature.'}
    },
    
    # Flashlight
    'ThingItemFlashlight': {
        'Low Power': {'modeValue': 'Low Power', 'description': 'Low power mode - dimmer light, longer battery life.'},
        'High Power': {'modeValue': 'High Power', 'description': 'High power mode - brighter light, faster battery drain.'}
    },
    
    # H2 Combustor
    'ThingH2Combustor': {
        'Idle': {'modeValue': 'Idle', 'description': 'Idle mode - combustor is not processing hydrogen.'},
        'Active': {'modeValue': 'Active', 'description': 'Active mode - combustor is burning hydrogen to generate heat.'}
    },
    
    # Plant Genetic Stabilizer
    'ThingAppliancePlantGeneticStabilizer': {
        'Stabilize': {'modeValue': 'Stabilize', 'description': 'Stabilize mode - locks plant genetics, preventing mutation during harvest.'},
        'Destabilize': {'modeValue': 'Destabilize', 'description': 'Destabilize mode - unlocks plant genetics, allowing mutation and cross-breeding.'}
    },
    
    # Hash Display
    'ThingCircuitboardHashDisplay': {
        'Prefab': {'modeValue': 'Prefab', 'description': 'Displays the PrefabHash of the target item (item type identifier).'},
        'GasLiquid': {'modeValue': 'GasLiquid', 'description': 'Displays the gas/liquid type hash of contents.'}
    },
    
    # Composite Roll Cover
    'ThingCompositeRollCover': {
        'Operate': {'modeValue': 'Operate', 'description': 'Manual operation mode - responds to direct Open/Close commands.'},
        'Logic': {'modeValue': 'Logic', 'description': 'Logic mode - controlled via logic network signals.'}
    },
    
    # LFO Volume
    'ThingDeviceLfoVolume': {
        'Whole Note': {'modeValue': 'Whole Note', 'description': 'Oscillation period: whole note duration (4 beats).'},
        'Half Note': {'modeValue': 'Half Note', 'description': 'Oscillation period: half note duration (2 beats).'},
        'Quarter Note': {'modeValue': 'Quarter Note', 'description': 'Oscillation period: quarter note duration (1 beat).'},
        'Eighth Note': {'modeValue': 'Eighth Note', 'description': 'Oscillation period: eighth note duration (half beat).'},
        'Sixteenth Note': {'modeValue': 'Sixteenth Note', 'description': 'Oscillation period: sixteenth note duration (quarter beat).'}
    },
    
    # Liquid Vacuum
    'ThingItemLiquidVacuum': {
        'Outward': {'modeValue': 'Outward', 'description': 'Expel mode - releases collected liquid into the environment.'},
        'Inward': {'modeValue': 'Inward', 'description': 'Collect mode - vacuums liquid from the environment into internal storage.'}
    },
    
    # Landing Pads
    'ThingLandingpad_CenterPiece01': {
        'None': {'modeValue': 'None', 'description': 'No landing pad status.'},
        'NoContact': {'modeValue': 'NoContact', 'description': 'Landing pad has no spacecraft contact.'},
        'Moving': {'modeValue': 'Moving', 'description': 'Spacecraft is approaching or departing.'},
        'Holding': {'modeValue': 'Holding', 'description': 'Spacecraft is holding position above pad.'},
        'Landed': {'modeValue': 'Landed', 'description': 'Spacecraft has successfully landed on pad.'}
    },
    'ThingLandingpad_DataConnectionPiece': {
        'None': {'modeValue': 'None', 'description': 'No landing pad status.'},
        'NoContact': {'modeValue': 'NoContact', 'description': 'Landing pad has no spacecraft contact.'},
        'Moving': {'modeValue': 'Moving', 'description': 'Spacecraft is approaching or departing.'},
        'Holding': {'modeValue': 'Holding', 'description': 'Spacecraft is holding position above pad.'},
        'Landed': {'modeValue': 'Landed', 'description': 'Spacecraft has successfully landed and data link active.'}
    },
    'ThingLandingpad_GasTankConnectorPiece': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Refuel mode - transfers fuel TO landed spacecraft.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Drain mode - transfers fuel FROM landed spacecraft.'}
    },
    
    # Power Transmitter
    'ThingStructurePowerTransmitter': {
        'Linked': {'modeValue': 'Linked', 'description': 'Transmitter is linked to a receiver and actively transmitting power.'},
        'Unlinked': {'modeValue': 'Unlinked', 'description': 'Transmitter has no active link to any receiver.'}
    },
    'ThingStructurePowerTransmitterReceiver': {
        'Linked': {'modeValue': 'Linked', 'description': 'Receiver is linked to a transmitter and receiving power.'},
        'Unlinked': {'modeValue': 'Unlinked', 'description': 'Receiver has no active link to any transmitter.'}
    },
    
    # Logic Transmitter
    'ThingStructureLogicTransmitter': {
        'Passive': {'modeValue': 'Passive', 'description': 'Passive mode - device only receives signals, does not broadcast.'}
    },
    
    # Weather Station
    'ThingStructureWeatherStation': {
        'InStorm': {'modeValue': 'InStorm', 'description': 'Weather station is currently within an active storm.'},
        'NoStorm': {'modeValue': 'NoStorm', 'description': 'Weather station detects clear weather, no storm present.'},
        'StormIncoming': {'modeValue': 'StormIncoming', 'description': 'Weather station detects a storm approaching.'}
    },
    
    # Solid Fuel Generator
    'ThingStructureSolidFuelGenerator': {
        'Generating': {'modeValue': 'Generating', 'description': 'Generator is actively burning fuel and producing power.'},
        'Not Generating': {'modeValue': 'Not Generating', 'description': 'Generator is idle, no fuel being consumed.'}
    },
    
    # Packer
    'ThingStructurePacker': {
        'Auto': {'modeValue': 'Auto', 'description': 'Automatic mode - packer operates automatically when items are available.'}
    },
    
    # Card Reader
    'ThingModularDeviceCardReader': {
        'Red': {'modeValue': 'Red', 'description': 'Accepts Red access cards only.'},
        'Blue': {'modeValue': 'Blue', 'description': 'Accepts Blue access cards only.'},
        'Green': {'modeValue': 'Green', 'description': 'Accepts Green access cards only.'},
        'Yellow': {'modeValue': 'Yellow', 'description': 'Accepts Yellow access cards only.'},
        'Orange': {'modeValue': 'Orange', 'description': 'Accepts Orange access cards only.'},
        'Purple': {'modeValue': 'Purple', 'description': 'Accepts Purple access cards only.'},
        'Pink': {'modeValue': 'Pink', 'description': 'Accepts Pink access cards only.'},
        'White': {'modeValue': 'White', 'description': 'Accepts White access cards only.'},
        'Black': {'modeValue': 'Black', 'description': 'Accepts Black access cards only.'},
        'Brown': {'modeValue': 'Brown', 'description': 'Accepts Brown access cards only.'},
        'Gray': {'modeValue': 'Gray', 'description': 'Accepts Gray access cards only.'},
        'Khaki': {'modeValue': 'Khaki', 'description': 'Accepts Khaki access cards only.'}
    },
    
    # Harvie Robot
    'ThingStructureHarvie': {
        'Happy': {'modeValue': 'Happy', 'description': 'Harvie is in happy mood - functioning well and content.'},
        'UnHappy': {'modeValue': 'UnHappy', 'description': 'Harvie is unhappy - may need attention or has encountered issues.'}
    },
    
    # Rocket Avionics
    'ThingStructureRocketAvionics': {
        'Survey': {'modeValue': 'Survey', 'description': 'Survey mode - scans destination for landing sites and resources.'},
        'Chart': {'modeValue': 'Chart', 'description': 'Chart mode - maps the destination terrain and features.'},
        'Deploy': {'modeValue': 'Deploy', 'description': 'Deploy mode - releases payload or equipment at destination.'},
        'Discover': {'modeValue': 'Discover', 'description': 'Discover mode - searches for new locations or phenomena.'},
        'Invalid': {'modeValue': 'Invalid', 'description': 'Invalid mode - destination or configuration error.'}
    },
    
    # Step Sequencer
    'ThingLogicStepSequencer8': {
        'Whole Note': {'modeValue': 'Whole Note', 'description': 'Step timing: whole note duration (slowest).'},
        'Half Note': {'modeValue': 'Half Note', 'description': 'Step timing: half note duration.'},
        'Quarter Note': {'modeValue': 'Quarter Note', 'description': 'Step timing: quarter note duration.'},
        'Eighth Note': {'modeValue': 'Eighth Note', 'description': 'Step timing: eighth note duration.'},
        'Sixteenth Note': {'modeValue': 'Sixteenth Note', 'description': 'Step timing: sixteenth note duration (fastest).'}
    },
    
    # Mining Charge
    'ThingItemMiningCharge': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Impact detonation - explodes on collision.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Remote detonation - explodes when triggered.'}
    },
    
    # Remote Detonator
    'ThingItemRemoteDetonator': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Arm mode - prepares charges for detonation.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Detonate mode - triggers armed charges.'}
    },
    
    # Plant Sampler
    'ThingItemPlantSampler': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Sample mode - extracts genetic sample from plant.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Insert mode - inserts genetic sample into plant.'}
    },
    
    # Terrain Manipulator
    'ThingItemTerrainManipulator': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Add terrain mode - places terrain material.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Remove terrain mode - removes terrain material.'}
    },
    
    # Advanced Tablet
    'ThingItemAdvancedTablet': {
        'Mode0': {'modeValue': 'Mode0', 'description': 'Default tablet mode.'},
        'Mode1': {'modeValue': 'Mode1', 'description': 'Secondary tablet mode.'}
    }
}

# Add modeDescriptions to devices
added_count = 0
for device_key, modes in device_mode_descriptions.items():
    if device_key in skip_devices:
        continue
    
    if device_key in device_index:
        idx = device_index[device_key]
        if 'modeDescriptions' not in desc_data['devices'][idx]:
            desc_data['devices'][idx]['modeDescriptions'] = modes
            added_count += 1
            print(f"Added modeDescriptions to {device_key}")
        else:
            print(f"Skipped {device_key} - already has modeDescriptions")
    else:
        print(f"Warning: {device_key} not found in descriptions.json")

# Save the updated file
with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'w', encoding='utf-8') as f:
    json.dump(desc_data, f, indent=2, ensure_ascii=False)

print(f"\nDone! Added modeDescriptions to {added_count} devices.")
