import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    sp_data = json.load(f)

# Missing devices to analyze
missing_devices = [
    'ThingItemMiningDrillPneumatic',
    'ThingItemGasCanisterSmart',
    'ThingItemLiquidCanisterSmart',
    'ThingDynamicGasTankAdvanced',
    'ThingDynamicAirConditioner',
    'ThingAppliancePlantGeneticStabilizer',
    'ThingCircuitboardHashDisplay',
    'ThingLandingpad_CenterPiece01',
    'ThingItemMiningCharge',
    'ThingStructureLogicTransmitter'
]

# Also check for more devices with Mode0/Mode1 that might need context
mode01_devices = []
for page in sp_data['pages']:
    if 'ModeInsert' in page and page['ModeInsert']:
        modes = [m.get('LogicName') for m in page['ModeInsert']]
        if 'Mode0' in modes and 'Mode1' in modes and len(modes) == 2:
            mode01_devices.append((page['Key'], page.get('Title', 'N/A')))

print("\n\n" + "="*60)
print("ALL DEVICES WITH ONLY Mode0/Mode1:")
print("="*60)
for key, title in sorted(mode01_devices):
    print(f"  {key}: {title}")

for page in sp_data['pages']:
    if page['Key'] in missing_devices:
        print(f"\n{'='*60}")
        print(f"Device: {page['Key']}")
        print(f"Title: {page.get('Title', 'N/A')}")
        
        if 'LogicInsert' in page and page['LogicInsert']:
            print(f"\nLogic Types ({len(page['LogicInsert'])}):")
            for logic in page['LogicInsert']:
                print(f"  - {logic.get('LogicName', 'Unknown')} (Access: {logic.get('LogicAccessTypes', 'N/A')})")
        
        if 'ModeInsert' in page and page['ModeInsert']:
            print(f"\nModes ({len(page['ModeInsert'])}):")
            for mode in page['ModeInsert']:
                print(f"  - {mode.get('LogicName', 'Unknown')}")
        
        if 'ConnectionInsert' in page and page['ConnectionInsert']:
            print(f"\nConnections ({len(page['ConnectionInsert'])}):")
            for conn in page['ConnectionInsert']:
                print(f"  - {conn.get('LogicName', 'Unknown')}")
        
        if 'SlotInsert' in page and page['SlotInsert']:
            print(f"\nSlots ({len(page['SlotInsert'])}):")
            for slot in page['SlotInsert']:
                print(f"  - {slot.get('SlotName', 'Unknown')} (Type: {slot.get('SlotType', 'N/A')})")
        
        if 'LogicSlotInsert' in page and page['LogicSlotInsert']:
            print(f"\nLogic Slots ({len(page['LogicSlotInsert'])}):")
            for ls in page['LogicSlotInsert']:
                print(f"  - Slot {ls.get('SlotIndex', '?')}: {ls.get('LogicName', 'Unknown')}")
