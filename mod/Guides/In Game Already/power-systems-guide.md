# Complete Power Systems Guide for Stationeers

A beginner-friendly guide covering power generation, storage, and distribution from early game through mid game.

**Data verified from:** Stationeers Wiki (January 2025)

---

## Section 1: Power Basics

### Power vs Energy

- **Watts (W)** = rate of power flow
- **Joules (J)** = stored energy
- 1 Watt = 1 Joule per second

### The Golden Rule

If machines demand more power than cables can handle, cables fry randomly. This is the #1 cause of "why did my base stop working?"

### Cable Types & Limits

| Cable | Max Power | Materials | Availability |
|-------|-----------|-----------|--------------|
| Normal | 5 kW | 0.5g Copper ingot | Early game |
| Heavy | 100 kW | 0.5g Copper + 0.5g Gold ingots | Early-mid game |
| Super Heavy | 500 kW | Super-alloys | Late game only |

### Basic Power Flow

```
Generator/Solar → Heavy Cable → Transformer/APC → Normal Cable → Devices
```

Never connect high-power sources directly to normal cables without protection.

### Protection Options

1. **Fuse** - Blows instead of random cables (5kW, 100kW, 500kW variants)
2. **Transformer** - Limits power draw (Small 5kW, Medium 25kW, Large 50kW)
3. **APC** - Limits + battery buffer for variable loads

### Key Components

- **Solar Panel**: Free power, 0-500W base (planet-dependent)
- **Station Battery**: Stores 3.6 MJ, unlimited charge/discharge rate
- **APC**: Distributes power with battery backup
- **Transformer**: Limits power, protects cables, blocks data signals

---

## Section 2: Solar Panels

### Part A: Manual Setup (Early Game)

**What You Need:**
- Solar Panel (any variant)
- 1 Glass Sheet (install after placement - won't work without it!)
- Cable Coil
- Wrench (for manual aiming)

**Placement:**
- Power port facing sunrise direction
- Data port faces 270° (sunset direction)
- Vertical range: 15-165° (90° = straight up)
- Horizontal range: 0-360°

**Manual Aiming:**
Use wrench to rotate. Check "Ratio" in panel stats - 1.0 = perfect alignment, 0.0 = no output.

**Power Output by Planet:**

| Planet | Max per Panel |
|--------|---------------|
| Vulcan | 500W - 1.2kW |
| Venus | 1.15kW |
| Moon | 750W |
| Mars | 455W |
| Europa | 210W |

**Early Game Tip:** On Moon, 6 panels at 750W = 4.5kW max, still safe on normal cables. 7+ panels need heavy cable.

---

### Part B: Automated Tracking (Basic Logic)

Manual aiming works but wastes power as sun moves. Automation keeps panels at optimal angle all day.

**What You Need:**
- Solar Panel (with data port - check variant!)
- Daylight Sensor
- 2x Logic Writer (or 1x Batch Writer)
- Cables for data network

**How It Works:**
1. Daylight Sensor outputs sun position (Horizontal + Vertical angles)
2. Logic Writers read sensor, write to solar panel
3. Panel automatically rotates to face sun

**Simple Setup (2 Writers):**

```
[Daylight Sensor] ←data cable→ [Logic Writer #1] ←data cable→ [Solar Panel]
                               [Logic Writer #2] ←data cable→ ↑
```

**Logic Writer #1 Settings:**
- Input: Daylight Sensor → Horizontal
- Output: Solar Panel → Horizontal

**Logic Writer #2 Settings:**
- Input: Daylight Sensor → Vertical
- Output: Solar Panel → Vertical

**Critical Note:** Sensor and panel orientation must match! If mounted differently, you'll need a Math Unit to offset the angles. Start with both on the same surface (e.g., both on ground, same facing direction).

---

### Part C: Batch Writer Method (Multiple Panels)

For 2+ panels, use a Batch Writer instead of individual Logic Writers:

**Batch Writer Settings:**
- Input: Daylight Sensor
- Output Type: Solar Panel (by hash)
- Variables: Horizontal, Vertical

This updates ALL solar panels on the network simultaneously.

**Power Budget:**
- Daylight Sensor: 10W
- Logic Writer: 10W each
- Batch Writer: 10W

Automation costs ~30W but maximizes output all day - worth it for 2+ panels.

---

## Section 3: Wind Turbines

### Upright Wind Turbine (Early Game)

The easier option - no steel required, just ores.

**Materials:** 10g Iron + 5g Gold + 10g Copper (Electronics Printer)

**Power Output:**

| Planet | Average | Max (Storm) |
|--------|---------|-------------|
| Mars | ~14W | ~43W |
| Europa | ~65W | ~175W |
| Venus | ~75W | ~175W |

**Best For:** Europa - where solar is weak (210W max) and wind is reliable.

**Behavior:**
- Output fluctuates in 1-2 minute sinusoidal cycles
- Storms dramatically increase output (up to 1kW)
- Works in thin atmospheres, better in dense ones

---

### Wind Turbine - Large (Mid Game)

Higher output but requires steel.

**Materials:** 20g Steel + 5g Electrum + 10g Copper (Electronics Printer Mk. II)

**Power Output:**
- Normal: Up to 500W
- Storms: Up to 20kW (!!)

**Construction:**
1. Place kit
2. Stage 1: 5x Steel Sheets + Welding
3. Stage 2: 5x Cable Coil
4. Stage 3: Screwdriver

---

### Storm Protection (Critical!)

Large turbines can spike to 20kW during storms. This WILL fry your cables if unprotected.

**Solutions:**

1. **Transformer** (Recommended)
   - Small transformer limits to 5kW - protects normal cables
   - Place between turbine and rest of network

2. **Heavy Cables Throughout**
   - 100kW limit handles any storm
   - More expensive (needs gold)

3. **Fuse**
   - Blows during overload instead of random cables
   - Must replace after each storm

**Recommended Setup:**

```
Wind Turbine → Heavy Cable → Small Transformer (5kW) → Normal Cable → Network
```

---

### When to Use Wind vs Solar

| Situation | Best Choice |
|-----------|-------------|
| Moon/Mars day | Solar |
| Europa | Wind (solar too weak) |
| Venus | Either (both strong) |
| Night backup | Wind + Battery |
| Storm-prone planet | Wind with transformer |

**Pro Tip:** Combine both - solar for day, wind fills gaps and storms charge batteries fast.

---

## Section 4: Solid Fuel Generator

### Overview

Backup/emergency power that burns fuel for electricity. Reliable but consumes resources.

**Power Output:** Up to 20kW (demand-dependent)

**Connection:**
- Up to 5kW demand: Normal cable OK
- Above 5kW: Requires heavy cable

---

### Fuel Types

| Fuel | Burn Time | Energy | Efficiency |
|------|-----------|--------|------------|
| Coal | 5 sec | 200 kJ | Baseline |
| Charcoal | 3 sec* | 120 kJ | Worst |
| Solid Fuel | 10 sec | 400 kJ | Best |

*Charcoal has 3-second minimum runtime regardless of listed 1-second burn.

**Warning:** Generator burns fuel at constant rate regardless of demand. If you only need 5kW but it's burning at 20kW rate, you're wasting energy.

---

### When to Use

**Good For:**
- Night backup when batteries run low
- Emergency power during repairs
- Early game before solar/wind established
- Temporary high-power needs (arc furnace)

**Not Good For:**
- Primary power (wastes fuel)
- Unattended operation (runs out, no warning)

---

### Setup Tips

**Basic Emergency Setup:**

```
Solid Fuel Generator → Heavy Cable → Battery → APC → Devices
```

**Automation Idea:** Use logic to only activate generator when battery drops below threshold (covered in advanced guide).

**Fuel Management:**
- Coal is abundant from mining
- Charcoal from furnace (ore → charcoal) - inefficient, use only if desperate
- Solid Fuel crafted later - best value per unit

---

## Section 5: Batteries & APCs

### Station Battery

Your power buffer - stores energy for night/storms/peak demand.

**Specs:**
- Capacity: 3.6 MJ (1 kWh)
- Charge/Discharge: Unlimited rate
- Self-consumption: 0W

**Passive Drain (Important!):**

| Condition | Drain |
|-----------|-------|
| Vacuum or <6.3 kPa | 50W loss |
| Pressurized, ≥0°C | 10W loss |
| Below 0°C | 10-50W (scales to absolute zero) |

**Placement Tip:** Keep batteries in pressurized, warm rooms to minimize drain. A battery in vacuum loses 50W constantly - that's 10% of a small solar setup wasted.

**Logic Outputs:**
- Ratio: 0.0-1.0 (charge percentage)
- Charge: Current energy in watt-tics
- PowerActual: Current output watts

---

### Area Power Controller (APC)

Creates isolated subnetworks with battery backup.

**What It Does:**
- Separates downstream devices from main grid
- Battery Cell buffers power fluctuations
- Pass-through when main power sufficient

**Power Use:** 10W self-consumption

**Modes (LED indicator):**

| Mode | Color | Meaning |
|------|-------|---------|
| 0 | Grey | Off or no battery |
| 1 | Red | No battery, drawing power |
| 2 | Blue-Red blink | Battery discharging |
| 3 | Blue-Green blink | Battery charging |
| 4 | Green | Battery full |

**Setup:**
1. Place APC on small grid
2. Open with crowbar
3. Insert Battery Cell
4. Switch to ON
5. Connect input (main power) and output (subnetwork)

---

### When to Use APC vs Transformer

| Use APC When | Use Transformer When |
|--------------|---------------------|
| Variable loads (Arc Furnace, Autolathe) | Constant loads (lights, vents) |
| Need battery backup | Just need cable protection |
| Want isolated subnetwork | Only limiting power draw |

---

### Early Game Power Layout

```
Solar Panels (Heavy Cable)
         ↓
   Station Battery
         ↓
        APC ──→ Normal Cable → Lights, basic machines
         ↓
   Transformer (5kW) → Normal Cable → Additional circuits
```

**Why This Works:**
- Battery buffers day/night cycle
- APC protects critical systems
- Transformer limits draw on branch circuits

---

## Section 6: Troubleshooting

### Cable Problems

| Problem | Cause | Fix |
|---------|-------|-----|
| Random cables burning | Exceeded cable limit | Add transformer/fuse, upgrade to heavy cable |
| Can't find burnt cable | Could be anywhere on overloaded network | Use Cable Analyzer, check near high-draw devices |
| Cables burn during storms | Wind turbine spike | Add transformer between turbine and network |

**Prevention:** Always use fuses. When they blow, you know exactly where the problem is.

---

### No Power Output

| Problem | Cause | Fix |
|---------|-------|-----|
| Solar panel shows 0W | No glass sheet installed | Install glass sheet |
| Solar panel shows 0W | Not aimed at sun | Check Ratio value, adjust with wrench |
| Solar at night = 0W | Normal behavior | Need battery storage |
| Wind turbine 0W | No atmosphere (inside/vacuum) | Must be outside in atmosphere |
| Generator not running | No fuel or not activated | Load fuel, check On state |

---

### Battery Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| Battery drains fast overnight | Too much passive drain | Move to pressurized warm room |
| Battery drains fast overnight | Devices drawing more than expected | Check actual power consumption |
| Battery not charging | Output exceeds input | Add more generation or reduce load |
| Battery not charging | Disconnected/damaged cable | Trace cable path |

---

### APC Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| APC grey/off | No battery cell or switched off | Insert battery, switch ON |
| APC red | No battery, drawing from main | Insert battery cell |
| APC constantly discharging | Insufficient main power | Add generation capacity |
| Devices behind APC not working | APC switched off or battery dead | Check APC state and charge |

---

### Quick Diagnostic Steps

1. **Check cable type** - Normal (5kW) vs Heavy (100kW)
2. **Check total draw** - Add up all device consumption
3. **Check generation** - Is it day? Windy? Fuel loaded?
4. **Check connections** - Trace cables, look for gaps/damage
5. **Check battery** - Ratio value shows charge level

---

## Quick Reference Card

### Power Generation

| Source | Output | Materials | Notes |
|--------|--------|-----------|-------|
| Solar Panel | 210W-1.2kW (planet) | Kit + Glass | Needs aiming/tracking |
| Upright Wind Turbine | 14-175W (1kW storm) | 10 Iron, 5 Gold, 10 Copper | Early game, no steel |
| Large Wind Turbine | Up to 500W (20kW storm) | 20 Steel, 5 Electrum, 10 Copper | Use transformer! |
| Solid Fuel Generator | Up to 20kW | Kit | Burns coal/charcoal/solid fuel |

### Cable Limits

| Cable | Limit | Use For |
|-------|-------|---------|
| Normal | 5 kW | Branch circuits, low-power devices |
| Heavy | 100 kW | Main lines, generators, batteries |
| Super Heavy | 500 kW | Late game trunk lines |

### Transformer Limits

| Size | Max Output |
|------|------------|
| Small | 5 kW |
| Medium | 25 kW |
| Large | 50 kW |

### Battery Drain

| Environment | Passive Loss |
|-------------|--------------|
| Vacuum | 50W |
| Pressurized, warm | 10W |
| Cold (<0°C) | 10-50W |

---

*Data verified from Stationeers Wiki (January 2025). Game updates may change values - if something doesn't work, check the in-game Stationpedia (F1).*
