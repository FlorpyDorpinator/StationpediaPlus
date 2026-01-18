#!/usr/bin/env python3
"""Merge converted entries into descriptions.json without overwriting existing entries"""
import json

# Read converted entries
with open('converted_entries.json', 'r', encoding='utf-8') as f:
    converted = json.load(f)

# Read existing descriptions
with open('descriptions.json', 'r', encoding='utf-8') as f:
    descriptions = json.load(f)

# Get existing keys to avoid duplicates
existing_guide_keys = {g.get('guideKey') for g in descriptions.get('guides', []) if g.get('guideKey')}
existing_mechanic_keys = {m.get('guideKey') for m in descriptions.get('mechanics', []) if m.get('guideKey')}

# Add the new guides (only if not already present)
added_guides = 0
if 'guides' in converted:
    for guide in converted['guides']:
        if guide.get('guideKey') and guide['guideKey'] not in existing_guide_keys:
            descriptions['guides'].append(guide)
            existing_guide_keys.add(guide['guideKey'])
            added_guides += 1
    print(f"Added {added_guides} new guides (skipped {len(converted['guides']) - added_guides} duplicates)")

# Add the new mechanics (only if not already present)
added_mechanics = 0
if 'mechanics' in converted:
    if 'mechanics' not in descriptions:
        descriptions['mechanics'] = []
    for mechanic in converted['mechanics']:
        if mechanic.get('guideKey') and mechanic['guideKey'] not in existing_mechanic_keys:
            descriptions['mechanics'].append(mechanic)
            existing_mechanic_keys.add(mechanic['guideKey'])
            added_mechanics += 1
    print(f"Added {added_mechanics} new mechanics (skipped {len(converted['mechanics']) - added_mechanics} duplicates)")

# Write the merged file
with open('descriptions.json', 'w', encoding='utf-8') as f:
    json.dump(descriptions, f, indent=2, ensure_ascii=False)

print('Successfully merged entries into descriptions.json')
