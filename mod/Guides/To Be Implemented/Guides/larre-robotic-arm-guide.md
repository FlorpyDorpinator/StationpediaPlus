# Complete LARRE Robotic Arm Guide for Stationeers

A comprehensive guide covering the LARRE (Linear Articulated Rail Entity) robotic arm system, including rail setup, dock variants, IC10 programming, and automation.

**Data verified from:** Decompiled game files (RoboticArmDock.cs, RoboticArmDockHydroponics.cs, RoboticArmDockCargo.cs, RoboticArmDockCollector.cs)

---

## Section 1: Introduction & Overview

LARRE is a robotic arm system that travels on rail tracks to automate tasks like plant harvesting, item transport, and collection. Unlike AIMEE which roams freely, LARRE follows a fixed rail network you design.

### System Components

| Component | Purpose |
|-----------|---------|
| Rail (Straight) | Track sections for arm travel |
| Rail (Corner) | Corner track sections |
| Junction | Stopping points where arm can act |
| Bypass | Points where arm can dock/undock |
| Dock | The robotic arm itself (multiple variants) |

### Dock Variants (From Game Code)

| Dock Type | Function | Slots |
|-----------|----------|-------|
| **Hydroponics** | Harvest, plant, fertilize plants | 2 (Hand + Extra) |
| **Cargo** | Move items to/from device slots | 1 (Hand) |
| **Collector** | Collect or eject items | 20 (Storage) |
| **Atmos** | Atmospheric operations | - |

### Key Concept

The LARRE arm travels between **junctions** on the rail network. You set the `TargetJunctionIndex` (Setting) to tell it where to go. When it arrives at a junction, it can extend down to perform actions on devices/items below.

---

## Section 2: Rail Network Setup

### Basic Network Layout

```
[Junction 0] --- [Rail] --- [Junction 1] --- [Rail] --- [Junction 2]
      |                                                      |
   [Bypass]                                              [Bypass]
      |                                                      |
   [Device]                                              [Device]
```

### Junction Numbering

- Junctions are numbered starting from 0
- The **starting dock** determines which junction is 0
- You can change the starting dock via the dock's interface (Button3)
- Junction numbering wraps for looped networks

### Looped vs Non-Looped Networks

**Non-Looped (Linear):**
- Arm travels back and forth
- Index clamped to min/max junction
- Simpler to set up

**Looped (Circular):**
- Arm can travel in either direction
- Index wraps around
- More efficient for many junctions

### Building Tips

1. Place rails on a ceiling or overhead structure
2. Junctions should be positioned over devices/plants
3. Bypass points allow the arm to dock and undock
4. Connect power to docks (not rails)

---

## Section 3: Hydroponics Dock

The Hydroponics LARRE automates plant care: harvesting, planting, and fertilizing.

### Slots

| Slot | Purpose |
|------|---------|
| Hand (0) | Primary item slot |
| Extra (1) | Overflow/secondary slot |

### Face States (Visual Indicator)

| State | Color | Meaning |
|-------|-------|---------|
| Idle | Grey | Not over a plant / Off / No power |
| Happy | Green | Plant healthy, can grow |
| UnHappy | Yellow | Plant has issues (dehydrated, bad gas, can't heal) |
| Dead | Red | Plant is dead |

### Behavior (From AnimateDownFinished)

**When hand is empty:**
- If harvestable plant exists: Harvest it (seeds if seeding, fruit if mature)
- If plant is dead/stuck: Destroy and clear it
- If fertilizer present but no plant: Destroy fertilizer
- Can also take items from ChuteExportBin

**When hand has item:**
- **Seed/Plant in hand + empty tray:** Plants it
- **Fertilizer in hand + tray with plant:** Fertilizes
- **Harvestable plant + stackable in hand:** Harvest if compatible
- Can also deposit items to ChuteBin

### Logic Variables (Readable)

| Variable | Description |
|----------|-------------|
| TargetSlotIndex | Index of target tray/device slot |
| TargetPrefabHash | Prefab hash of target device |

### Logic Variables (Per Slot, Slot 255 = Proxy Target)

| Variable | Description |
|----------|-------------|
| Efficiency | Growth efficiency (0-1) |
| Health | Plant health (0-1) |
| Growth | Current growth stage |
| Mature | 1 if mature, 0 otherwise |
| Seeding | 1 if seeding with seeds, 0 otherwise, -1 if no plant |
| MaturityRatio | Progress to maturity (0-1) |
| SeedingRatio | Progress to seeding (0-1) |
| HarvestedHash | Prefab hash of what will be harvested |

---

## Section 4: Cargo Dock

The Cargo LARRE moves items to and from device slots (lockers, containers, etc.).

### Slot

| Slot | Purpose |
|------|---------|
| Hand (0) | Item being moved |

### Target Slot Index

- Range: 0-50
- Set via buttons on dock or via logic (TargetSlotIndex)
- Determines which slot of the target device to interact with

### Behavior (From DoContextualAction)

**When hand is empty:**
- Takes item from target device's slot (CurrentSlotIndex)

**When hand has item:**
- If target slot empty and accepts item type: Deposits item
- If target slot occupied and swappable: Swaps items

### Slot Access Rules (From CanAccessSlot)

Cannot access slots that are:
- Plant-type slots
- Non-interactable slots
- Locked slots
- Hidden occupant slots (internal slots)

### Logic Variables

| Variable | R/W | Description |
|----------|-----|-------------|
| Setting | R/W | Target junction index |
| TargetSlotIndex | R/W | Target device slot (0-50) |
| TargetPrefabHash | R | Prefab hash of target device |
| PositionX | R | Current junction index |
| Idle | R | 1 if idle, 0 otherwise |
| Extended | R | 1 if arm down, 0 otherwise |

---

## Section 5: Collector Dock

The Collector LARRE collects items from the world or ejects items from its storage.

### Slots

- 20 storage slots
- Fill indicator shows capacity (checks slots 5, 12, 19)

### Modes (VentDirection)

| Mode | Value | Behavior |
|------|-------|----------|
| Outward | 0 | Ejects items from storage |
| Inward | 1 | Collects nearby items into storage |

### Behavior (From DoContextualArmAction)

**Mode = Inward (Collecting):**
- Attracts nearby items with force (300-600N based on distance)
- Collection range: 1 meter from arm tip
- If over ChuteExportBin: Takes items from export bin
- Retracts automatically when full

**Mode = Outward (Ejecting):**
- Ejects items at 0.2-0.4 second intervals
- If over ChuteBin/ChuteInlet: Deposits to chute
- Retracts automatically when empty

### Logic Variables

| Variable | R/W | Description |
|----------|-----|-------------|
| Setting | R/W | Target junction index |
| Mode | R/W | VentDirection (0=Out, 1=In) |
| Ratio | R | Fill ratio (0-1) |
| Quantity | R | Number of items (0-20) |
| PositionX | R | Current junction index |
| Idle | R | 1 if idle, 0 otherwise |
| Extended | R | 1 if arm down, 0 otherwise |

---

## Section 6: Common Logic Variables (All Docks)

### Readable Variables

| Variable | Description |
|----------|-------------|
| Setting | Target junction index |
| PositionX | Current junction index (-1 if moving) |
| Idle | 1 if stationary and arm up, 0 otherwise |
| Extended | 1 if arm is down, 0 otherwise |

### Writable Variables

| Variable | Description |
|----------|-------------|
| Setting | Set target junction index |
| Activate | 1 to trigger arm action |
| Open | 0 to close (dock at bypass), 1 to open (undock) |

### Arm States (Internal)

| State | Description |
|-------|-------------|
| Up | Arm retracted, can travel |
| Down | Arm extended, can act |
| AnimatingUp | Arm retracting |
| AnimatingDown | Arm extending |

---

## Section 7: IC10 Programming

### Basic Hydroponics Automation

```mips
alias LARRE db

# Junction indices
define JUNCTION_CHUTE 0
define JUNCTION_TRAY1 1
define JUNCTION_TRAY2 2
define JUNCTION_TRAY3 3

main:
    yield

    # Wait until idle
    l r0 LARRE Idle
    beqz r0 main

    # Check if we have items to deposit
    ls r0 LARRE 0 Occupied  # Hand slot
    bgtz r0 goDeposit

    # Find a harvestable plant
    push ra
    jal findHarvest
    pop ra

    j main

findHarvest:
    # Check tray 1
    s LARRE Setting JUNCTION_TRAY1
    push ra
    jal waitAndCheck
    pop ra
    bgtz r0 doHarvest

    # Check tray 2
    s LARRE Setting JUNCTION_TRAY2
    push ra
    jal waitAndCheck
    pop ra
    bgtz r0 doHarvest

    # Check tray 3
    s LARRE Setting JUNCTION_TRAY3
    push ra
    jal waitAndCheck
    pop ra
    bgtz r0 doHarvest

    j ra

waitAndCheck:
    yield
    l r0 LARRE Idle
    beqz r0 waitAndCheck

    # Check if plant is mature (slot 255 = proxy)
    ls r0 LARRE 255 Mature
    j ra

doHarvest:
    # Activate to extend and harvest
    s LARRE Activate 1
    push ra
    jal waitIdle
    pop ra
    j ra

goDeposit:
    s LARRE Setting JUNCTION_CHUTE
    push ra
    jal waitIdle
    pop ra
    s LARRE Activate 1
    push ra
    jal waitIdle
    pop ra
    j main

waitIdle:
    yield
    l r0 LARRE Idle
    beqz r0 waitIdle
    j ra
```

### Cargo Dock Item Mover

```mips
alias LARRE db

define SOURCE_JUNCTION 0
define DEST_JUNCTION 1
define SOURCE_SLOT 0
define DEST_SLOT 5

main:
    yield
    l r0 LARRE Idle
    beqz r0 main

    # Check if hand is empty
    ls r0 LARRE 0 Occupied
    bgtz r0 deposit

pickup:
    # Go to source
    s LARRE Setting SOURCE_JUNCTION
    jal waitIdle

    # Set target slot
    s LARRE TargetSlotIndex SOURCE_SLOT

    # Activate to pick up
    s LARRE Activate 1
    jal waitIdle
    j main

deposit:
    # Go to destination
    s LARRE Setting DEST_JUNCTION
    jal waitIdle

    # Set target slot
    s LARRE TargetSlotIndex DEST_SLOT

    # Activate to deposit
    s LARRE Activate 1
    jal waitIdle
    j main

waitIdle:
    yield
    l r0 LARRE Idle
    beqz r0 waitIdle
    j ra
```

### Collector Inward/Outward Control

```mips
alias LARRE db

define COLLECTION_JUNCTION 0
define CHUTE_JUNCTION 1

main:
    yield
    l r0 LARRE Idle
    beqz r0 main

    # Check fill level
    l r0 LARRE Quantity
    bge r0 15 goUnload  # If 15+ items, go unload

    # Go collect
    s LARRE Mode 1  # Inward
    s LARRE Setting COLLECTION_JUNCTION
    jal waitIdle
    s LARRE Activate 1
    jal waitIdle
    j main

goUnload:
    s LARRE Mode 0  # Outward
    s LARRE Setting CHUTE_JUNCTION
    jal waitIdle
    s LARRE Activate 1
    jal waitIdle
    j main

waitIdle:
    yield
    l r0 LARRE Idle
    beqz r0 waitIdle
    j ra
```

---

## Section 8: Troubleshooting

### Movement Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Arm won't move | Not powered or off | Check power connection and OnOff state |
| Arm stops mid-travel | Obstruction detected | Clear path, check for blocking objects |
| Arm stuck at junction | Error state | Check Error variable, clear obstruction |
| Wrong junction numbers | Starting dock changed | Set correct dock as starting dock |

### Action Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Arm won't extend | Face blocked | Clear structure below arm |
| Arm won't extend | Currently moving | Wait for Idle = 1 before activating |
| Hydroponics not harvesting | Plant not mature | Check Mature slot variable |
| Cargo not picking up | Slot locked/hidden | Use accessible slot index |
| Collector full | Mode is Inward | Set Mode to 0 (Outward) to eject |
| Collector empty | Mode is Outward | Set Mode to 1 (Inward) to collect |

### Network Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Rails not connected | Gap in network | Place rails end-to-end |
| Junction count wrong | Network reconfigured | Rail network auto-updates on change |
| Arm teleports | Network rebuilt | Normal after network update |

### Error Codes

| Error | Cause | Solution |
|-------|-------|----------|
| Error = 1 | Obstructed | Clear obstacle near arm |
| FaceBlocked | Structure below | Remove wall/structure under arm |

---

## Section 9: Setup Checklist

### Initial Setup

1. **Plan layout** - Determine where devices/trays will be
2. **Build overhead structure** - Rails need ceiling/mounting
3. **Place rails** - Connect junctions with straight/corner rails
4. **Place junctions** - Over each target device
5. **Place dock** - This is the arm itself
6. **Set starting dock** - Button3 on dock
7. **Connect power** - To the dock

### Hydroponics Setup

1. Complete initial setup
2. Place hydroponic trays under junctions
3. Place ChuteBin or ChuteExportBin at deposit junction
4. Program IC or use manual control
5. Give arm seeds to plant (or harvest existing)

### Cargo Setup

1. Complete initial setup
2. Place devices (lockers, etc.) under junctions
3. Set TargetSlotIndex appropriately
4. Program IC for automated transfer

### Collector Setup

1. Complete initial setup
2. Place collection area under junction
3. Place ChuteBin/ChuteInlet at unload junction
4. Set Mode (0=Out, 1=In)
5. Program IC for automated collection

---

## Quick Reference Card

### Dock Types

| Type | Purpose | Slots |
|------|---------|-------|
| Hydroponics | Plant care | 2 |
| Cargo | Item transfer | 1 |
| Collector | Item collection/ejection | 20 |

### Common Logic (All Docks)

| Read | Write |
|------|-------|
| Setting | Setting |
| PositionX | Activate |
| Idle | Open |
| Extended | - |

### Collector Modes

| Mode | Value | Action |
|------|-------|--------|
| Outward | 0 | Eject items |
| Inward | 1 | Collect items |

### Cargo Slot Range

- TargetSlotIndex: 0-50

### Key Commands

```mips
# Read current position
l r0 LARRE PositionX

# Set target junction
s LARRE Setting 2

# Trigger action
s LARRE Activate 1

# Wait for idle
l r0 LARRE Idle
beqz r0 wait

# Read Cargo target slot
l r0 LARRE TargetSlotIndex

# Set Cargo target slot
s LARRE TargetSlotIndex 5

# Read Collector fill
l r0 LARRE Quantity

# Set Collector mode
s LARRE Mode 1
```

---

*Data extracted from decompiled game files: RoboticArmDock.cs, RoboticArmDockHydroponics.cs, RoboticArmDockCargo.cs, RoboticArmDockCollector.cs. Values are authoritative as of game version current to January 2026.*
