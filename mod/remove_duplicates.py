import json

with open('descriptions.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find and remove duplicate entries (keep first occurrence)
seen_keys = set()
filtered_devices = []
for device in data['devices']:
    key = device.get('deviceKey')
    if key in seen_keys:
        print(f'Removing duplicate: {key}')
    else:
        seen_keys.add(key)
        filtered_devices.append(device)

print(f'Removed {len(data["devices"]) - len(filtered_devices)} duplicates')
data['devices'] = filtered_devices

with open('descriptions.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=2, ensure_ascii=False)

print('Done!')
