# Complete AIMEE Mining Bot Guide for Stationeers

A comprehensive guide covering the AIMEE autonomous mining robot, including setup, IC10 programming, modes, and troubleshooting.

**Data verified from:** Decompiled game files (RobotMining.cs, RobotMode.cs)

---

## Section 1: Introduction & Overview

AIMEE (Autonomous Intelligent Mining and Exploration Entity) is a programmable mining robot that autonomously collects surface ores. Unlike manual mining or deep miners, AIMEE roams the surface finding and collecting exposed ore deposits.

### Key Specifications (From Game Code)

| Spec | Value | Source |
|------|-------|--------|
| Max Speed | 3 m/s | `MaxSpeed = 3f` |
| Search Radius | 32 meters | `MinableSearchArea = 32` |
| Mining Depth | 2 layers (surface only) | `maxMiningDepth = 2` |
| Unload Range | 3 meters | `GetNearestRobotInput(..., 3f)` |
| Stuck Detection | 60 seconds | `_isStuckCheckAmount = 60f` |
| Stuck Threshold | 0.1m movement | `_isStuckMovementAmount = 0.1f` |

### What It Can Do

- **Mine surface ores** - Any ore visible on or near the surface (within 2 layers deep)
- **Navigate autonomously** - Uses pathfinding when programmed
- **Store mined ores** - Internal storage slots hold collected ore
- **Unload to chutes** - Deposits ores into Chute Inlet or Chute Bin
- **Be fully programmable** - IC10 chip slot for custom behavior
- **Be repaired** - Uses Duct Tape for repairs

### What It Cannot Do

- Mine deep underground deposits (depth > 2)
- Select specific ore types to mine
- Navigate reliably over significant terrain height changes
- Operate indefinitely without battery recharging

---

## Section 2: Slots & Power

### Slot Configuration

AIMEE has three types of slots:

| Slot | Purpose | Contents |
|------|---------|----------|
| Battery Slot | Power source | Battery Cell (any size) |
| Chip Slot | Programming | Programmable Chip |
| Storage Slots | Ore storage | Mined ores |

**Important:** The battery slot is checked first when reading `ChargeRatio` via logic. Access it via slot index 0.

### Power Consumption (From Game Code)

AIMEE's power draw is calculated per game tick:

```
Total Power = Base Power + Motor Power + Chip Power
```

| Component | Power Draw | Condition |
|-----------|------------|-----------|
| Base (Idle) | 5W | When OnOff = true |
| Motor | CurrentMotorPower x 10W | When moving |
| Chip Execution | 2.5W | Per chip execution cycle (128 ops) |

**Practical Power Usage:**
- Stationary with chip running: ~7.5W average
- Moving at full speed: ~30-50W (depends on terrain)
- Following/roaming: Variable, 15-50W typical

### Battery Recommendations

| Battery Cell | Capacity | Approx Runtime (Idle) |
|--------------|----------|----------------------|
| Small | 36 kJ | ~2 hours |
| Large | 3.6 MJ | ~8+ hours |

**Tip:** Use a Large Battery Cell for extended autonomous operation. AIMEE can be programmed to return for charging when battery drops below a threshold.

---

## Section 3: Modes

AIMEE operates in distinct modes controlled via the `Mode` logic variable.

### Mode Reference (From RobotMode.cs)

| Mode | Value | Description |
|------|-------|-------------|
| None/Idle | 0 | Stopped, not operating |
| Follow | 1 | Follows the nearest player |
| MoveToTarget | 2 | Moves to TargetX/Y/Z coordinates |
| Roam | 3 | Autonomously searches for and mines ore |
| Unload | 4 | Deposits ores into nearby Chute Inlet/Bin |
| PathToTarget | 5 | Uses pathfinding to reach TargetX/Y/Z |
| StorageFull | 6 | READ ONLY - Indicates storage is full |

### Mode Details

**Mode 0 (Idle):**
- AIMEE stops all movement
- Power consumption drops to base 5W + chip
- Use to pause operations or during setup

**Mode 1 (Follow):**
- Follows the nearest player
- Good for guiding AIMEE to new locations
- No mining occurs in this mode

**Mode 2 (MoveToTarget):**
- Moves directly toward TargetX/Y/Z
- Does NOT use pathfinding
- Can get stuck on obstacles or height differences
- Stops when close to target

**Mode 3 (Roam):**
- Autonomous mining mode
- Searches 32-meter radius for surface ores
- Mines any ore found within 2 layers of surface
- Roams randomly when no ore nearby
- **This is the primary mining mode**

**Mode 4 (Unload):**
- Attempts to deposit ores into nearby Chute Inlet or Chute Bin
- Must be within 3 meters of a valid input
- Returns to Mode 0 (Idle) when empty or no valid input

**Mode 5 (PathToTarget):**
- Uses pathfinding to reach TargetX/Y/Z
- Better at navigating around obstacles than Mode 2
- Still struggles with major terrain height changes
- More reliable for longer distances

**Mode 6 (StorageFull):**
- READ ONLY - automatically set when storage is full
- Indicates AIMEE needs to unload
- Program should detect this and initiate unload sequence

---

## Section 4: Logic Variables

### Readable Variables (From GetLogicValue)

| Variable | Type | Description |
|----------|------|-------------|
| PositionX | float | Current X world coordinate |
| PositionY | float | Current Y world coordinate (height) |
| PositionZ | float | Current Z world coordinate |
| VelocityMagnitude | float | Current speed (m/s) |
| VelocityX/Y/Z | float | Velocity components |
| VelocityRelativeX/Y/Z | float | Relative velocity components |
| ForwardX/Y/Z | float | Forward direction vector |
| Orientation | float | Current heading (degrees) |
| MineablesInVicinity | int | Count of ores within search radius |
| PressureExternal | float | External atmosphere pressure |
| TemperatureExternal | float | External atmosphere temperature |

### Writable Variables (From SetLogicValue)

| Variable | Type | Description |
|----------|------|-------------|
| TargetX | float | Target X coordinate for movement |
| TargetY | float | Target Y coordinate (height) |
| TargetZ | float | Target Z coordinate |

### Slot Variables

Access battery status via slot 0:

| Variable | Description |
|----------|-------------|
| Charge | Current battery charge (joules) |
| ChargeRatio | Battery percentage (0.0 - 1.0) |
| MaxCharge | Battery capacity (joules) |

---

## Section 5: Unloading System

### Compatible Devices

AIMEE can unload into any device implementing `IRobotInput`:

| Device | Description |
|--------|-------------|
| Chute Inlet | Standard item input point |
| Chute Bin | Container with item input |

### Unload Requirements

1. AIMEE must be within **3 meters** of the input device
2. The input slot must be empty (can accept item)
3. The device must allow input (`AllowInput = true`)
4. Set AIMEE to Mode 4 (Unload)

### Unload Process

1. AIMEE checks for nearest `IRobotInput` within 3m
2. Transfers one item at a time from storage to input
3. Continues until storage is empty
4. Returns to Mode 0 (Idle) when complete
5. If no valid input found, immediately returns to Mode 0

### Chute Setup Example

```
AIMEE → Chute Inlet → Chute → Sorting System
                 ↓
            (3m max)
```

Place the Chute Inlet facing AIMEE's parking/unload location. Connect chutes to your ore sorting or storage system.

---

## Section 6: IC10 Programming

### Basic AIMEE Program Structure

```mips
alias AIMEE db          # AIMEE set as device on batch

# Constants
define MODE_IDLE 0
define MODE_FOLLOW 1
define MODE_MOVETOTARGET 2
define MODE_ROAM 3
define MODE_UNLOAD 4
define MODE_PATHTOTARGET 5
define MODE_STORAGEFULL 6

# Unload location coordinates
define UNLOAD_X -100
define UNLOAD_Y 50
define UNLOAD_Z 200

main:
    # Check if storage is full
    l r0 AIMEE Mode
    beq r0 MODE_STORAGEFULL unload

    # Check battery level (slot 0)
    ls r0 AIMEE 0 ChargeRatio
    blt r0 0.2 returnToBase    # Return if below 20%

    # Check if there are ores nearby
    l r0 AIMEE MineablesInVicinity
    bgt r0 0 mining

    # No ores - roam to find more
    s AIMEE Mode MODE_ROAM
    j main

mining:
    s AIMEE Mode MODE_ROAM
    yield
    j main

unload:
    # Set target to unload location
    s AIMEE TargetX UNLOAD_X
    s AIMEE TargetY UNLOAD_Y
    s AIMEE TargetZ UNLOAD_Z
    s AIMEE Mode MODE_PATHTOTARGET

waitForArrival:
    yield
    l r0 AIMEE VelocityMagnitude
    bgt r0 0.1 waitForArrival

    # At destination - unload
    s AIMEE Mode MODE_UNLOAD

waitForUnload:
    yield
    l r0 AIMEE Mode
    beq r0 MODE_IDLE doneUnloading
    j waitForUnload

doneUnloading:
    j main

returnToBase:
    # Navigate to charging station
    s AIMEE TargetX UNLOAD_X
    s AIMEE TargetY UNLOAD_Y
    s AIMEE TargetZ UNLOAD_Z
    s AIMEE Mode MODE_PATHTOTARGET

    # Wait for arrival then idle for charging
waitCharge:
    yield
    l r0 AIMEE VelocityMagnitude
    bgt r0 0.1 waitCharge
    s AIMEE Mode MODE_IDLE

charging:
    yield
    ls r0 AIMEE 0 ChargeRatio
    blt r0 0.9 charging    # Wait until 90% charged
    j main
```

### Key Programming Patterns

**Check if at destination:**
```mips
l r0 AIMEE VelocityMagnitude
blt r0 0.1 atDestination    # Stopped or nearly stopped
```

**Wait for mode change:**
```mips
waitLoop:
    yield
    l r0 AIMEE Mode
    bne r0 expectedMode waitLoop
```

**Read battery from slot:**
```mips
ls r0 AIMEE 0 ChargeRatio    # 0 = battery slot
```

---

## Section 7: Troubleshooting

### Movement Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Won't move | Mode set to 0 (Idle) | Set Mode to 1, 2, 3, or 5 |
| Won't move | Battery empty | Charge or replace battery |
| Won't move | No chip or chip error | Check chip installation and code |
| Stuck dancing | Height difference at target | Y coordinate unreachable - see below |
| Moves slowly | Low battery | Charge battery |
| Goes wrong direction | Wrong target coordinates | Verify TargetX/Y/Z values |

### The Y-Coordinate Problem

AIMEE struggles when the target Y coordinate doesn't match terrain height:

**Symptoms:**
- AIMEE lifts front or rear wheels, "dancing"
- Never reaches destination
- Gets stuck indefinitely

**Cause:** MoveToTarget and PathToTarget try to reach the exact Y coordinate. If terrain is higher or lower, AIMEE can't physically reach it.

**Solutions:**
1. Use waypoints at known good elevations
2. Set TargetY slightly above expected terrain
3. Use Follow mode to manually guide over difficult terrain
4. Place unload stations on flat ground

### Mining Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Not mining | Not in Roam mode (3) | Set Mode to 3 |
| Not mining | No surface ores in 32m radius | Move to new location |
| Not mining | Ores too deep (>2 layers) | Ores must be surface or near-surface |
| Storage full | Mode 6 active | Trigger unload sequence |
| MineablesInVicinity = 0 | Area depleted | Relocate to new mining zone |

### Unloading Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Won't unload | Too far from Chute Inlet/Bin | Must be within 3 meters |
| Won't unload | Chute input slot occupied | Ensure chute is processing items |
| Won't unload | Chute not powered/enabled | Check chute power and settings |
| Returns to Idle immediately | No valid input found | Position closer to chute input |

### Power Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Drains fast | High motor usage (terrain) | Operate on flatter terrain |
| Drains fast | Chip running heavy loops | Optimize chip code, add yields |
| Won't charge | Not at charging station | Create charging dock with APC |
| Won't charge | Battery full | Check ChargeRatio = 1.0 |

---

## Section 8: Setup Checklist

### Initial Deployment

1. **Craft AIMEE Kit** from Electronics Printer
2. **Place and build** AIMEE on relatively flat ground
3. **Insert Battery Cell** (Large recommended)
4. **Insert Programmable Chip** (optional but recommended)
5. **Set Mode to 3** (Roam) for basic autonomous mining
6. **Place Chute Inlet** within 3m of intended unload spot

### Automated Setup

1. Complete initial deployment
2. **Program chip** with unload/return logic
3. **Set up charging station** (APC with battery near unload point)
4. **Test unload sequence** - verify AIMEE can reach and deposit
5. **Monitor first few cycles** for stuck conditions
6. **Establish waypoints** if terrain is problematic

### Recommended Base Layout

```
[Mining Zone]     32m+      [Unload/Charge Station]
                    ↑
    AIMEE roams  ←→ flat path ←→  Chute Inlet
    in this area                    ↓
                              Ore Processing
                                    ↓
                              Charging Dock (APC)
```

---

## Quick Reference Card

### Mode Values

| Value | Mode | Use |
|-------|------|-----|
| 0 | Idle | Stopped |
| 1 | Follow | Follow player |
| 2 | MoveToTarget | Direct movement |
| 3 | Roam | **Mining mode** |
| 4 | Unload | Deposit ores |
| 5 | PathToTarget | Pathfinding movement |
| 6 | StorageFull | Read-only flag |

### Key Constants

| Constant | Value |
|----------|-------|
| Max Speed | 3 m/s |
| Search Radius | 32 meters |
| Mining Depth | 2 layers |
| Unload Range | 3 meters |
| Stuck Detection | 60 seconds |

### Power Draw

| State | Power |
|-------|-------|
| Idle (OnOff) | 5W |
| Motor (max) | +30W |
| Chip cycle | +2.5W |
| **Typical moving** | **15-50W** |

### Critical Logic Variables

| Read | Write |
|------|-------|
| Mode | Mode |
| PositionX/Y/Z | TargetX/Y/Z |
| VelocityMagnitude | - |
| MineablesInVicinity | - |
| (Slot 0) ChargeRatio | - |

---

*Data extracted from decompiled game files: RobotMining.cs, RobotMode.cs. Values are authoritative as of game version current to January 2026.*
