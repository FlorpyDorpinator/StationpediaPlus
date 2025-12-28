import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    sp_data = json.load(f)

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\mod\descriptions.json', 'r', encoding='utf-8') as f:
    desc_data = json.load(f)

# Get devices that already have modeDescriptions
devices_with_modes = set()
for device in desc_data.get('devices', []):
    if device.get('modeDescriptions'):
        devices_with_modes.add(device['deviceKey'])

print(f"Devices already with modeDescriptions: {len(devices_with_modes)}")
print(f"  {sorted(devices_with_modes)}")
print()

# Find devices in Stationpedia that have ModeInsert but we don't have descriptions for
devices_needing_modes = []
for page in sp_data['pages']:
    if 'ModeInsert' in page and page['ModeInsert']:
        key = page['Key']
        title = page.get('Title', key)
        if key not in devices_with_modes:
            modes = [m.get('LogicName', 'Unknown') for m in page['ModeInsert']]
            devices_needing_modes.append((key, title, modes))

print(f"Devices with modes but NO modeDescriptions: {len(devices_needing_modes)}")
print()
for key, title, modes in sorted(devices_needing_modes)[:30]:
    print(f"  {key}: {modes}")

if len(devices_needing_modes) > 30:
    print(f"  ... and {len(devices_needing_modes) - 30} more")
