import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    sp_data = json.load(f)

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'r', encoding='utf-8') as f:
    desc_data = json.load(f)

# Get all existing generic modes
existing_modes = set(desc_data['genericDescriptions']['modes'].keys())

# Get all modes from Stationpedia
all_modes = {}
for page in sp_data['pages']:
    if 'ModeInsert' in page and page['ModeInsert']:
        for mode in page['ModeInsert']:
            mode_name = mode.get('LogicName', 'Unknown')
            if mode_name not in all_modes:
                all_modes[mode_name] = []
            all_modes[mode_name].append(page['Key'])

# Find missing modes
missing_modes = {}
for mode, devices in all_modes.items():
    if mode not in existing_modes:
        missing_modes[mode] = devices

print(f"Total unique modes in Stationpedia: {len(all_modes)}")
print(f"Existing generic modes: {len(existing_modes)}")
print(f"Missing modes: {len(missing_modes)}")
print()

# Group by category
print("=== MISSING MODES ===")
for mode in sorted(missing_modes.keys()):
    devices = missing_modes[mode]
    print(f"  {mode}: used by {len(devices)} devices")
    if len(devices) <= 3:
        print(f"    Devices: {', '.join(devices)}")
