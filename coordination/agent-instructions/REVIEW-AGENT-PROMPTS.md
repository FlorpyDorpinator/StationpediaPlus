# Review Agent Prompt Template

Copy and customize for each agent (replace {N} with batch number 1-16):

---

## Agent {N} - Review Batch {N}

Read the review instructions at:
```
C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\agent-instructions\REVIEW-INSTRUCTIONS.md
```

Your assigned devices are listed in:
```
C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs\review-batch-{N}.json
```

Reference files:
- **Stationpedia data**: `Stationeers/Stationpedia/Stationpedia.json` (has all LogicInsert arrays)
- **Source code**: `Assembly-CSharp/` folder (verify Mode/Setting/etc)
- **Original descriptions**: `coordination/outputs/batch-*-output.json`

Your tasks:
1. For each device in your batch, find ALL logic types from Stationpedia.json
2. Add descriptions for every MISSING logic type
3. VERIFY Mode/Setting/Ratio/Error/Lock/Activate descriptions against the game's source code in the workspace
4. Use standard descriptions ONLY for generic types that likely don't change per device (Power, Temperature, RatioOxygen, etc)

Output your complete device entries to:
```
C:\Dev\12-17-25 Stationeers Respawn Update Code\StationpediaPlus\coordination\outputs\review-batch-{N}-complete.json
```

Format - output a JSON array with COMPLETE logicDescriptions for each device:
```json
[
  {
    "deviceKey": "ThingStructureActiveVent",
    "displayName": "Active Vent",
    "logicDescriptions": {
      "Power": { "dataType": "Boolean", "range": "0-1", "description": "Returns 1 if powered." },
      "On": { "dataType": "Boolean", "range": "0-1", "description": "Enable/disable vent." },
      "Mode": { "dataType": "Integer", "range": "0-1", "description": "0=Inward, 1=Outward." },
      // ... ALL logic types for this device
    }
  }
]
```

Work autonomously. Read source code for Mode/Setting. Complete all devices before reporting.

---

## Batch Assignments Summary

| Batch | Devices | Work |
|-------|---------|------|
| 1 | 17 | 238 |
| 2 | 17 | 238 |
| 3 | 17 | 238 |
| 4 | 17 | 237 |
| 5 | 17 | 237 |
| 6 | 17 | 237 |
| 7 | 17 | 236 |
| 8 | 17 | 237 |
| 9 | 18 | 236 |
| 10 | 17 | 235 |
| 11 | 17 | 235 |
| 12 | 18 | 237 |
| 13 | 18 | 237 |
| 14 | 18 | 237 |
| 15 | 18 | 237 |
| 16 | 18 | 237 |
