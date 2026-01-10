# Airlock Systems Guide

A comprehensive guide to building and configuring automated airlocks in Stationeers.

---

## Overview

An airlock is a chamber between two doors that prevents atmosphere from escaping when you enter or exit your base. Stationeers provides two circuitboard types for automated airlock control:

| Type | Use Case | Circuitboard |
|------|----------|--------------|
| **Basic Airlock** | Atmosphere ↔ Vacuum (Moon, space) | Circuitboard (Airlock) |
| **Advanced Airlock** | Atmosphere ↔ Atmosphere (Mars, Europa) | Circuitboard (Advanced Airlock) |

**The key difference:** Basic airlocks cycle between pressure and vacuum. Advanced airlocks cycle between two different atmospheres (your base atmosphere and the planet's atmosphere).

---

## When to Use Which

### Basic Airlock (Atmosphere to Vacuum)
- **Moon:** No atmosphere outside (vacuum)
- **Space stations:** Vacuum environment
- **Any vacuum environment**

The airlock depressurizes completely before opening the exterior door.

### Advanced Airlock (Atmosphere to Atmosphere)
- **Mars:** CO2 atmosphere outside
- **Europa:** Thin atmosphere
- **Loulan:** High-pressure atmosphere
- **Any planet with atmosphere**

The airlock exchanges your base atmosphere for exterior atmosphere (and vice versa) to prevent mixing.

---

## Components Required

### Basic Airlock Components

| Component | Quantity | Purpose |
|-----------|----------|---------|
| Doors (any type) | 2 | Interior and exterior access |
| Console | 1 | Control interface |
| Circuitboard (Airlock) | 1 | Basic airlock logic |
| Active Vent | 1 | Pressurize/depressurize chamber |
| Gas Sensor | 1 | Monitor chamber pressure |
| Glass Sheet | 1 | Complete console |
| Pipe Kits | 2+ | Connect vent to base atmosphere |
| Cable Coils | As needed | Power and data network |

**Optional:**
- Flashing Light (visual indicator during cycling)
- APC with battery (emergency backup power)
- Passive Vent (connects pipe to base atmosphere)

### Advanced Airlock Components

| Component | Quantity | Purpose |
|-----------|----------|---------|
| Doors (any type) | 2 | Interior and exterior access |
| Console | 1+ | Control interface (can add slaves) |
| Circuitboard (Advanced Airlock) | 1+ | Advanced airlock logic |
| Active Vent | 2 | One for interior, one for exterior |
| Gas Sensor | 1 | Monitor chamber pressure |
| Glass Sheet | 1 per console | Complete consoles |
| Pipe Kits | 4+ | Separate interior and exterior pipe networks |
| Cable Coils | As needed | Power and data network |

**Optional:**
- Slave consoles (control from outside the airlock)
- Flashing Light
- APC with battery

---

## Door Types

Several door types work with airlock systems:

| Door Type | Materials | Power | Pressure Rating | Notes |
|-----------|-----------|-------|-----------------|-------|
| **Composite Door** | Door Kit + Plastic + Glass | 10W | 300 kPa | Standard, from starter kit |
| **Glass Door** | Door Kit + (varies) | 10W | 200 kPa | High visibility |
| **Airlock (Structure)** | Airlock Kit + 2 Steel + 2 Plastic | 25W | 1 MPa | Heavy duty, keypads on both sides |
| **Blast Door** | (crafted) | varies | High | Industrial use |

**For beginners:** Composite Doors from your starter kit work fine. The Airlock (Structure) requires Steel, which needs smelting.

---

## Basic Airlock: Step-by-Step Setup

### Step 1: Build the Chamber

1. Create a small enclosed room (2x1 or 3x1 grid cells is sufficient)
2. The chamber needs airtight walls on all sides
3. Leave openings for two doors - one facing interior, one facing exterior

### Step 2: Install Doors

1. Place **Door 1** facing your base interior
2. Place **Door 2** facing exterior (vacuum)
3. **IMPORTANT:** Before configuration, ensure:
   - Exterior door is **OPEN**
   - Interior door is **CLOSED**

**Tip:** Use the Labeller to name your doors "Interior Door" and "Exterior Door" for easy identification.

### Step 3: Install Active Vent

1. Place the **Active Vent** inside the airlock chamber on the small grid
2. The vent needs pipe connection to your base atmosphere

### Step 4: Set Up Piping

**Simple Setup (for vacuum airlocks):**
```
[Base Interior] - [Passive Vent] - [Pipe] - [Active Vent in Airlock]
```

1. Place a **Passive Vent** inside your base (connects base atmosphere to pipe)
2. Run **Pipe** from the Passive Vent to the Active Vent in the airlock
3. The pipe network will carry atmosphere to/from the airlock

### Step 5: Install Gas Sensor

1. Place the **Gas Sensor** inside the airlock chamber
2. It monitors pressure to determine when cycling is complete

### Step 6: Build the Console

1. Place the **Console Kit** on a wall (inside or just outside the airlock)
2. Insert the **Circuitboard (Airlock)** into the console
3. Cover with a **Glass Sheet** to complete

### Step 7: Wire Everything Together

**All devices must be on the same cable network:**
- Console
- Both doors
- Active Vent
- Gas Sensor
- Power source

Run cables connecting all components. Cables carry both power and data.

### Step 8: Configure the Console

**Configuration Method (Current - Screwdriver):**

As of the Respawn Update, you can configure using a screwdriver on the side screw of the console.

**Configuration Method (Legacy - Data Disk):**

1. Insert a **Data Disk** into the console
2. The console enters configuration mode
3. All connected devices appear on screen

**Selection Order is CRITICAL:**

1. **Select EXTERIOR door FIRST** - This assigns it as the exterior portal
2. Select Interior door second
3. Select Active Vent
4. Select Gas Sensor
5. Optionally select Light

**The first door you select becomes the EXTERIOR door.**

6. Remove the Data Disk (or exit config mode with screwdriver)

### Step 9: Test the Airlock

1. Interact with the console
2. Press **Cycle to Exterior** - Should depressurize, then open exterior door
3. Press **Cycle to Interior** - Should pressurize to ~101 kPa, then open interior door

---

## Advanced Airlock: Step-by-Step Setup

The advanced airlock is more complex because it manages two different atmospheres.

### Key Difference: Two Pipe Networks

You need **separate, unconnected pipe networks**:
- **Interior pipe network:** Connects to your base atmosphere
- **Exterior pipe network:** Vents to outside atmosphere

**These must NOT be connected to each other!**

### Step 1: Build the Chamber

Same as basic airlock - create an enclosed chamber with two door openings.

### Step 2: Install Doors

1. Place Interior door (facing base)
2. Place Exterior door (facing outside)
3. **Before configuration:**
   - Exterior door: **OPEN**
   - Interior door: **CLOSED**

### Step 3: Install Two Active Vents

1. Place **Interior Active Vent** in the airlock chamber
2. Place **Exterior Active Vent** in the airlock chamber

**Tip:** Label them "Interior Vent" and "Exterior Vent" using the Labeller.

### Step 4: Set Up Two Separate Pipe Networks

**Interior Pipe Network:**
```
[Base Interior] - [Passive Vent] - [Pipe] - [Interior Active Vent]
```

**Exterior Pipe Network:**
```
[Exterior Active Vent] - [Pipe] - [Passive Vent] - [Outside Atmosphere]
```

**CRITICAL:** These two pipe networks must be completely separate. If they connect, atmospheres will mix.

### Step 5: Install Gas Sensor

Place inside the airlock chamber to monitor pressure during cycling.

### Step 6: Build the Console

1. Place Console Kit
2. Insert **Circuitboard (Advanced Airlock)**
3. Cover with Glass Sheet

### Step 7: Wire Everything

Connect all components to the same cable network:
- Console
- Both doors
- Both Active Vents
- Gas Sensor
- Power source

### Step 8: Configure the Console

**Selection Order for Advanced Airlock:**

1. **Exterior Door FIRST**
2. **Exterior Vent**
3. Interior Door
4. Interior Vent
5. Gas Sensor
6. Optional: Light, Slave Consoles

**Set Pressure Thresholds:**

The advanced airlock needs to know target pressures:
- **Interior pressure:** Your base pressure (typically ~100 kPa)
- **Exterior pressure:** Outside atmosphere pressure (varies by planet)

On Mars, exterior might be ~0.6-1 kPa. Set these values in the configuration.

### Step 9: Test Both Directions

1. **Cycle to Interior:** Should fill airlock with base atmosphere, then open interior door
2. **Cycle to Exterior:** Should replace with exterior atmosphere, then open exterior door

---

## Console Configuration Details

### Using the Screwdriver (Current Method)

1. With screwdriver in hand, click on the small screw on the side of the console
2. Configuration interface opens
3. Select devices in proper order (exterior first!)
4. Click screw again to exit configuration

### Using the Data Disk (Legacy Method)

1. Insert Data Disk into console slot
2. Configuration interface opens
3. Devices on the same cable network appear as selectable (green)
4. Click devices to select/assign them
5. Remove Data Disk to save and exit

### Resetting the Circuitboard

If configuration gets corrupted or you need to start over:

1. Use crowbar to remove glass sheet from console
2. Remove the circuitboard
3. **Use screwdriver on the circuitboard** to factory reset it
4. Reinstall circuitboard and glass
5. Reconfigure from scratch

---

## Slave Consoles (Advanced Airlock Only)

Slave consoles let you control the airlock from multiple locations (e.g., both sides of the airlock).

### Setting Up Slave Consoles

1. Build additional consoles with **Advanced Airlock Circuitboard**
2. Wire them to the same network as the master console
3. In the master console configuration, select the slave consoles under "Make Slave"

### Known Issues with Slaves

- Slave consoles may appear grayed out and non-functional
- This is a known bug - slaves sometimes don't work properly
- **Workaround:** Use IC10 programming for multi-point control, or accept single-console operation

---

## Common Problems and Solutions

### "ERROR IN CONFIG"

**Causes:**
- Door states were wrong during configuration (both open, both closed, or reversed)
- Selected items in wrong order
- Circuitboard is confused from previous configuration

**Solutions:**
1. Ensure exterior door is OPEN, interior door is CLOSED before configuring
2. Reset the circuitboard with screwdriver
3. Reconfigure, selecting exterior door FIRST
4. If error persists: deconstruct console, reset circuitboard, rebuild

### Airlock Won't Cycle / Doors Stay Locked

**Causes:**
- No power to the system
- Devices not on same cable network
- Configuration incomplete

**Solutions:**
1. Verify power is reaching all components
2. Check cable connections - all devices need to be networked together
3. Use screwdriver to reset and reconfigure

### Doors Operate Backwards (Interior/Exterior Reversed)

**Causes:**
- Selected doors in wrong order during configuration
- Game reload bug (known issue)

**Solutions:**
1. Reset circuitboard and reconfigure, carefully selecting exterior door first
2. Ensure door states are correct before configuring (exterior open, interior closed)

### Atmosphere Leaking Through Airlock

**Causes:**
- Airlock opens before fully depressurized
- Gas sensor placed incorrectly or not detecting properly
- Large airlock with insufficient sensors

**Solutions:**
1. Ensure gas sensor is inside the airlock chamber
2. For large airlocks, add additional gas sensors (one per grid section)
3. Verify pipe network has enough capacity

### Advanced Airlock Mixing Atmospheres

**Causes:**
- Interior and exterior pipe networks are connected
- Wrong vents assigned to wrong networks

**Solutions:**
1. Verify pipe networks are completely separate
2. Check that interior vent connects to base, exterior vent connects to outside
3. Label everything clearly with the Labeller

### Can't Operate Airlock After Power Loss

**Causes:**
- When powered, the circuitboard locks all doors and vents
- Lock persists even after power is lost

**Solutions:**
- **Prevention:** Use an APC with battery cell dedicated to the airlock
- **Emergency:** Deconstruct the console to release the lock, manually operate doors with crowbar

---

## Best Practices

### Power Safety

**CRITICAL:** When powered, the airlock circuitboard locks all connected doors and vents. They cannot be manually operated, even after power is lost.

**Recommendation:** Always use an APC with a battery cell dedicated to your airlock system. This provides backup power if your main grid fails.

### Labeling

Use the Labeller to name:
- Interior Door / Exterior Door
- Interior Vent / Exterior Vent
- Airlock Console

This prevents confusion during configuration and troubleshooting.

### Door State Before Configuration

Always set door states before entering configuration mode:
- **Exterior door: OPEN**
- **Interior door: CLOSED**

This helps the circuitboard correctly identify which is which.

### Sensor Placement

- Place gas sensor inside the airlock chamber
- For large airlocks (multiple grid cells), use one sensor per cell
- Sensor must detect when pressure reaches target before doors unlock

### Pipe Sizing

For faster cycling:
- Use larger pipe networks with more volume
- Ensure passive vents connect to adequately sized rooms
- Don't undersize pipes for large airlocks

### Emergency Access

Always have a backup plan:
- APC with battery for emergency power
- Know how to deconstruct the console if needed
- Consider a manual door elsewhere as emergency exit

---

## Quick Reference: Configuration Order

### Basic Airlock
1. Exterior Door (FIRST - must be open)
2. Interior Door (must be closed)
3. Active Vent
4. Gas Sensor
5. Light (optional)

### Advanced Airlock
1. Exterior Door (FIRST - must be open)
2. Exterior Vent
3. Interior Door (must be closed)
4. Interior Vent
5. Gas Sensor
6. Light (optional)
7. Slave Consoles (optional)

---

## Power Requirements

| Component | Power Draw |
|-----------|------------|
| Console | 50W |
| Composite Door | 10W each |
| Airlock (Structure) | 25W |
| Active Vent | 100W each |
| Gas Sensor | 1W |

**Basic Airlock Total:** ~170W minimum
**Advanced Airlock Total:** ~270W minimum (with 2 vents)

Ensure your power network can handle peak demand during cycling.

---

## Crafting Requirements

### Circuitboard (Airlock) - Basic
- Crafted at: Electronics Printer or Fabricator
- Materials: (check in-game, varies by recipe version)

### Circuitboard (Advanced Airlock)
- Crafted at: Electronics Printer or Fabricator
- Materials: 1g Iron, 5g Gold, 5g Copper

### Console Kit
- Crafted at: Electronics Printer or Fabricator
- Materials: 2g Iron, 5g Copper, 3g Gold

---

## Summary

1. **Choose the right type:** Basic for vacuum, Advanced for atmosphere
2. **Set door states correctly:** Exterior open, interior closed
3. **Select exterior first:** Always configure exterior door/vent before interior
4. **Keep pipe networks separate:** Advanced airlocks need isolated interior/exterior pipes
5. **Use backup power:** APC with battery prevents lockouts
6. **Label everything:** Makes troubleshooting much easier
7. **Reset if confused:** Screwdriver on circuitboard factory resets it

---

**Sources:**
- [Unofficial Stationeers Wiki - Circuitboard (Airlock)](https://stationeers-wiki.com/Circuitboard_(Airlock))
- [Unofficial Stationeers Wiki - Circuitboard (Advanced Airlock)](https://stationeers-wiki.com/Circuitboard_(Advanced_Airlock))
- [Unofficial Stationeers Wiki - Guide (Airlock) Atmosphere to Vacuum](https://stationeers-wiki.com/Guide_(Airlock)_Atmosphere_to_Vacuum)
- [Unofficial Stationeers Wiki - Guide (Airlock) Atmosphere to Atmosphere](https://stationeers-wiki.com/Guide_(Airlock)_Atmosphere_to_Atmosphere)
- [Unofficial Stationeers Wiki - Console](https://stationeers-wiki.com/Console)
- [Unofficial Stationeers Wiki - Active Vent](https://stationeers-wiki.com/Active_Vent)
- [Unofficial Stationeers Wiki - Gas Sensor](https://stationeers-wiki.com/Gas_Sensor)
- [Unofficial Stationeers Wiki - Passive Vent](https://stationeers-wiki.com/Passive_Vent)
- [Unofficial Stationeers Wiki - Airlock (Structure)](https://stationeers-wiki.com/Airlock_(Structure))
- [Steam Community Discussions](https://steamcommunity.com/app/544550/discussions/)
