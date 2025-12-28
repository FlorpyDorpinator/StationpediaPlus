import json

with open(r'c:\Dev\12-17-25 Stationeers Respawn Update Code\Stationeers\Stationpedia\Stationpedia.json', 'r', encoding='utf-8') as f:
    sp_data = json.load(f)

# All devices we need to add
devices_to_add = [
    'ThingItemMiningDrillPneumatic',
    'ThingItemGasCanisterSmart',
    'ThingItemLiquidCanisterSmart',
    'ThingDynamicGasTankAdvanced',
    'ThingDynamicAirConditioner',
    'ThingAppliancePlantGeneticStabilizer',
    'ThingCircuitboardHashDisplay',
    'ThingLandingpad_CenterPiece01',
    'ThingItemMiningCharge',
    'ThingStructureLogicTransmitter',
    # Mode0/Mode1 devices that need context
    'ThingLandingpad_LiquidTankConnectorPiece',
    'ThingPortableComposter',
    'ThingStructureAdvancedComposter',
    'ThingStructureAdvancedFurnace',
    'ThingStructureFurnace',
    'ThingStructureCamera',
    'ThingStructureSecurityCameraFishEye',
    'ThingStructureSecurityCameraLeft',
    'ThingStructureSecurityCameraPanning',
    'ThingStructureSecurityCameraRight',
    'ThingStructureSecurityCameraStraight',
    'ThingStructureSpotlight',
    'ThingStructureGlowLight2',
    'ThingStructureHorizontalAutoMiner',
    'ThingStructureSDBSilo',
    'ThingModularDeviceLabelDiode2',
    'ThingModularDeviceLabelDiode3',
    'ThingModularDeviceNumpad',
    'ThingStructureChuteFlipFlopSplitter',
    'ThingStructureChuteDigitalFlipFlopSplitterLeft',
    'ThingStructureChuteDigitalFlipFlopSplitterRight',
    'ThingStructurePipeOrgan'
]

for page in sp_data['pages']:
    if page['Key'] in devices_to_add:
        print(f"\n{'='*70}")
        print(f"Key: {page['Key']}")
        print(f"Title: {page.get('Title', 'N/A')}")
        
        if 'LogicInsert' in page and page['LogicInsert']:
            print(f"\nLogic Types:")
            for logic in page['LogicInsert']:
                access = logic.get('LogicAccessTypes', '0')
                access_str = 'Read' if access == '0' else 'Read/Write' if access == '2' else f'Access:{access}'
                print(f"  {logic.get('LogicName', '?')}: {access_str}")
        
        if 'ModeInsert' in page and page['ModeInsert']:
            print(f"\nModes:")
            for mode in page['ModeInsert']:
                print(f"  - {mode.get('LogicName', '?')}")
