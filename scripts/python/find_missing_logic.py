#!/usr/bin/env python3
"""
Find devices that are missing logic descriptions by comparing
Stationpedia.json (source of truth) with descriptions.json (our data).
"""

import json
import os

def load_json(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        return json.load(f)

def extract_logic_types(logic_insert):
    """Extract logic type names from LogicInsert array."""
    types = []
    for item in logic_insert:
        # Extract name from rich text like "<link=LogicTypeSetting><color=orange>Setting</color></link>"
        name = item.get('LogicName', '')
        # Simple extraction - get text between last > and </
        if '>' in name and '</' in name:
            start = name.rfind('>') 
            # Find the last > before </
            parts = name.split('</')[0]
            if '>' in parts:
                name = parts.split('>')[-1]
        types.append(name)
    return types

def main():
    # Paths
    script_dir = os.path.dirname(os.path.abspath(__file__))
    base_dir = os.path.dirname(os.path.dirname(script_dir))
    
    stationpedia_path = os.path.join(base_dir, '..', 'Stationeers', 'Stationpedia', 'Stationpedia.json')
    descriptions_path = os.path.join(base_dir, 'mod', 'descriptions.json')
    
    print(f"Loading Stationpedia from: {stationpedia_path}")
    print(f"Loading descriptions from: {descriptions_path}")
    
    stationpedia = load_json(stationpedia_path)
    descriptions = load_json(descriptions_path)
    
    # Build lookup of our device descriptions
    our_devices = {}
    for device in descriptions.get('devices', []):
        key = device.get('deviceKey', '')
        our_devices[key] = device.get('logicDescriptions', {})
    
    # Get generic logic descriptions
    generic_logic = descriptions.get('genericDescriptions', {}).get('logic', {})
    
    # Track missing
    missing_report = []
    total_missing = 0
    
    # Check each page in Stationpedia
    for page in stationpedia.get('pages', []):
        key = page.get('Key', '')
        logic_insert = page.get('LogicInsert', [])
        
        if not logic_insert:
            continue
            
        # Get what logic types this device has
        stationpedia_types = extract_logic_types(logic_insert)
        
        # Get what we have for this device
        our_logic = our_devices.get(key, {})
        
        # Find missing
        missing_types = []
        for logic_type in stationpedia_types:
            if logic_type not in our_logic and logic_type not in generic_logic:
                missing_types.append(logic_type)
        
        if missing_types:
            missing_report.append({
                'device': key,
                'title': page.get('Title', ''),
                'missing': missing_types,
                'has_device_entry': key in our_devices
            })
            total_missing += len(missing_types)
    
    # Output report
    print(f"\n{'='*60}")
    print(f"MISSING LOGIC DESCRIPTIONS REPORT")
    print(f"{'='*60}")
    print(f"Total devices with missing types: {len(missing_report)}")
    print(f"Total missing logic type entries: {total_missing}")
    print(f"{'='*60}\n")
    
    # Sort by number of missing types (most first)
    missing_report.sort(key=lambda x: len(x['missing']), reverse=True)
    
    for item in missing_report:
        has_entry = "✓ Has entry" if item['has_device_entry'] else "✗ No entry"
        print(f"\n{item['device']} ({item['title']}) [{has_entry}]")
        print(f"  Missing: {', '.join(item['missing'])}")
    
    # Also output as JSON for easy processing
    output_path = os.path.join(script_dir, '..', '..', 'coordination', 'outputs', 'missing-logic-report.json')
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(missing_report, f, indent=2)
    print(f"\n\nFull report saved to: {output_path}")

if __name__ == '__main__':
    main()
