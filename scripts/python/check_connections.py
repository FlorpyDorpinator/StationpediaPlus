import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Find a device with connections
count = 0
for page in data['pages']:
    if 'ConnectionInsert' in page and page['ConnectionInsert'] and count < 5:
        print(f"Device: {page['Key']}")
        print(f"Connections: {page['ConnectionInsert']}")
        print()
        count += 1
