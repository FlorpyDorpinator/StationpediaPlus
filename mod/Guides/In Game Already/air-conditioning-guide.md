# Air Conditioning Guide

From emergency cooling to full climate control systems.

---

## STOP - Read This First

### Your Starter Kit Contains a Bomb

The **Liquid Nitrogen Canister** that comes with your Kit (Portable Air Conditioner) is wrapped in insulation. The moment you unwrap it, a countdown to explosion begins.

**Here's what happens:**

1. You unwrap the canister - the insulation disappears permanently
2. Liquid nitrogen starts warming up (whether you use it or not)
3. As liquid nitrogen warms, it phase-changes to gas
4. Gas takes up FAR more volume than liquid
5. The canister's burst pressure is approximately **6 MPa**
6. The expanding nitrogen exceeds this limit
7. **The canister explodes**

**This happens no matter what you do:**

| Action | Result |
|--------|--------|
| Unwrap and leave it sitting | Warms → Explodes |
| Insert into Portable AC and store | Slows warming → Still explodes |
| Actually use it for cooling | Phase change → Overpressure → Explodes |

The only exception: on extremely cold planets (Europa), the canister might stay cold enough *outside* - but the Portable AC is useless outside, and won't function without the canister.

**This is not a "be careful" warning. This is a "your base WILL be destroyed" warning.**

### The Safe Solution

**Do not unwrap the canister until you have built the following:**

1. A pipe network with adequate tank capacity (higher pressure rating)
2. A **Portables Connector** to link the Portable AC to your pipe network

When the Portable AC is connected to a proper pipe network via Portables Connector, the phase-changing nitrogen vents into infrastructure that can handle the pressure instead of exploding inside the canister.

We'll cover exactly how to set this up in the Portable AC section below.

---

## Part 1: Portable Air Conditioner [BEGINNER]

The Portable Air Conditioner is your first temperature control option. It's battery-powered and can both heat and cool.

### How It Works

The Portable AC moves heat between your room and its internal storage:

- **Cold Mode:** Pulls heat OUT of the room, stores it internally
- **Hot Mode:** Releases stored heat INTO the room

**Specifications:**

| Stat | Value |
|------|-------|
| Power Draw | 200W (battery-powered) |
| Heat Transfer | 4,000 J/s |
| Internal Tank | 250L volume |
| Max Internal Pressure | 8,106 kPa |

**Battery Life:**
- Small Battery: ~1.5 minutes
- Large Battery: ~12 minutes
- Nuclear Battery: ~1 hour 36 minutes

### Operating Limits

The Portable AC has temperature limits where it stops working:

- **Cold Mode:** Errors if room exceeds **+30°C** (303K)
- **Hot Mode:** Errors if room drops below **-10°C** (263K)

If you see an error, the room temperature is outside the operating range.

### The Internal Storage Problem

When using the Portable AC standalone (no pipe connection):

1. Heat accumulates in the internal tank
2. As the tank fills, pressure builds
3. When pressure hits maximum, the AC flashes an error
4. You must vent the tank before continuing

**For Cold Mode:** The internal tank gets HOT. You need to dump this heat somewhere.

**For Hot Mode:** The internal tank gets COLD. Same problem, opposite direction.

This makes standalone operation impractical for sustained use.

### The Correct Setup: Portables Connector

To use the Portable AC safely and sustainably, connect it to a pipe network.

**Components Needed:**

| Component | Purpose |
|-----------|---------|
| Portables Connector | Links portable devices to pipe network |
| Pipe Kits (4+) | Build the pipe network |
| Pipe Radiator (2+) | Dissipate heat to atmosphere |
| Tank (optional) | Buffer capacity for pressure spikes |
| Wrench | Connect the Portable AC |

**Setup Steps:**

1. **Build the pipe network first:**
   - Place Portables Connector on small grid (inside or outside your base)
   - Run pipes from the Portables Connector to outside your base
   - Place Pipe Radiators on the outside section (they must not touch walls/floor)
   - Create a loop back to the Portables Connector (circulation helps)

2. **Pressurize the network:**
   - You need gas in the pipes for heat transfer
   - Use a Portable Tank or canister to add gas to the network
   - ~50-100 kPa of any gas works; CO2 or Pollutants transfer heat faster

3. **Connect the Portable AC:**
   - Place the Portable AC near the Portables Connector
   - Use your **Wrench** on the Portable AC to attach it
   - The AC now vents to your pipe network instead of internal storage

4. **NOW unwrap and insert the nitrogen canister:**
   - With the pipe network ready, phase-changing nitrogen vents safely
   - Monitor your pipe network pressure
   - The radiators outside will dissipate excess heat/pressure

**Diagram:**
```
[Inside Base]                    [Outside Base]

Portable AC ─── Portables ═══════╗
              Connector          ║
                                 ╠══ Pipe Radiator
                                 ║
                   ╔═════════════╝
                   ║
                   ╚══ Pipe Radiator

(═══ = pipes, must loop back for circulation)
```

### Why This Works

The Portables Connector acts as a bridge. Instead of pressure building inside the tiny canister (6 MPa limit), it vents into your pipe network which has:

- Much larger volume (more pipes = more volume)
- Higher pressure tolerance (pipes handle more than canisters)
- Radiators actively dissipating heat/pressure outside

The nitrogen still phase-changes, but now it has somewhere safe to go.

---

## Part 2: Wall-Mounted Air Conditioner [INTERMEDIATE]

Once you can craft advanced components, the wall-mounted Air Conditioner provides proper climate control with a built-in thermostat.

**Crafting Requirements:**
- Kit (Atmospherics) - crafted at Fabricator or Electronics Printer
- Materials vary by recipe; requires Steel and advanced components

### How It Works

The Air Conditioner has three ports:

| Port | Function |
|------|----------|
| **Input** | Room air enters here |
| **Output** | Conditioned air exits here (heated or cooled) |
| **Waste** | Coolant loop - carries away excess heat |

**The AC acts as its own pump** - it pulls air through Input and pushes it out Output. You don't need additional pumps for the room air side.

**Power Consumption:**
- Idle: 10W
- Active: 350W

### The Dual Efficiency System

This is where most players get confused. The AC has **three efficiency factors** that multiply together:

1. **OTE** - Operational Temperature Efficiency
2. **TDE** - Temperature Differential Efficiency
3. **PE** - Pressure Efficiency

**If ANY of these is near zero, the AC does almost nothing.**

---

### Quick Rules (Do This)

**For good Pressure Efficiency:**
- Keep room pressure above **100 kPa**
- Keep waste loop pressure above **100 kPa**
- Low-pressure environments (Mars surface, unpressurized rooms) kill efficiency

**For good Temperature Differential Efficiency:**
- Keep the temperature difference between Input and Waste under **50°C**
- Every degree of difference costs ~1% efficiency
- At 100°C+ difference, efficiency drops to 0%

**For good Operational Temperature Efficiency:**
- Works best between **-50°C and 100°C**
- Extreme temperatures (400°C+) severely degrade efficiency
- At 1000°C, efficiency hits 0%

**The Practical Rule:**
> For every 50°C of cooling/heating you need, plan one Air Conditioner in series.

Trying to cool a 150°C room with one AC connected to a -50°C waste loop won't work well. The 200°C differential kills your efficiency.

---

### Why This Works (The Mechanics)

**Pressure Efficiency (PE):**

The AC needs gas molecules to move heat. Low pressure = fewer molecules = less heat transfer capacity.

The formula considers whichever is lower: your Input pressure or your Waste pressure. If your waste loop is at vacuum, PE is 0% regardless of room pressure.

**Temperature Differential Efficiency (TDE):**

Heat naturally flows from hot to cold. The AC is essentially a heat pump fighting against or assisting this natural flow.

- Cooling with a cold waste loop = working WITH physics = high efficiency (can exceed 100%)
- Cooling with a hot waste loop = fighting physics = low efficiency
- The bigger the temperature gap, the harder the AC works, the less efficient it becomes

**Operational Temperature Efficiency (OTE):**

The AC's internal components have optimal operating ranges. Extreme temperatures stress the system.

- Below -50°C or above 100°C, efficiency starts dropping
- This is why you can't just dump superheated furnace exhaust through an AC

---

### Reference Build: Basic Room Cooling

This setup cools a single room using exterior radiators.

**Components:**

| Component | Quantity | Purpose |
|-----------|----------|---------|
| Kit (Atmospherics) | 1 | The AC unit |
| Passive Vent | 2 | Room air intake and return |
| Pipe Radiator | 4+ | Heat rejection (outside) |
| Pipe Kits | 10+ | Piping network |
| Volume Pump (optional) | 1 | Force waste circulation |

**Build Steps:**

1. **Place the AC** on a wall with access to both inside and outside

2. **Room Air Loop (Input/Output):**
   ```
   [Room] ←── Passive Vent ←── AC Output
                               ↑
   [Room] ──→ Passive Vent ──→ AC Input
   ```
   - Passive Vent inside room connects to AC Input
   - AC Output connects to another Passive Vent inside room
   - Room air circulates through the AC

3. **Waste Loop (outside):**
   ```
   AC Waste ──→ Pipes ──→ Radiators ──→ Pipes ──→ back to AC Waste
   ```
   - Run pipes from AC Waste port to outside
   - Place Pipe Radiators on the outside section
   - Loop back to AC Waste port (closed loop)
   - Optional: Add Volume Pump to force circulation

4. **Pressurize the Waste Loop:**
   - Use Portable Tank with CO2 or Pollutants
   - Connect via Tank Connector or Portables Connector
   - Fill to ~200-500 kPa
   - CO2 and Pollutants have best thermal conductivity

5. **Set Target Temperature:**
   - Interact with the AC
   - Set your desired temperature (e.g., 20°C / 293K)
   - The built-in thermostat handles the rest

**Diagram:**
```
        INSIDE                    OUTSIDE
    ┌─────────────┐          ┌──────────────┐
    │   [ROOM]    │          │              │
    │             │          │  ══╦═══╦══   │
    │  ┌───┐      │    WALL  │    ║   ║     │
    │  │ V ├──────┼────AC────┼────╝   ╚───┐ │
    │  └───┘      │  WASTE   │            │ │
    │      ┌──────┼────AC────┼──Radiators─┘ │
    │  ┌───┤      │  IN/OUT  │              │
    │  │ V │      │          │              │
    │  └───┘      │          │              │
    └─────────────┘          └──────────────┘

    V = Passive Vent
```

### Troubleshooting

**AC shows low efficiency but radiators are cold:**
- Check room pressure - if below 100 kPa, efficiency suffers
- Pressurize your room to 100+ kPa

**AC shows 0% Operational Temperature Efficiency:**
- Your waste loop temperature is wrong for the mode
- For cooling: waste loop must be COLDER than room
- For heating: waste loop must be HOTTER than room

**AC runs but room temperature doesn't change:**
- Check all three efficiencies - if any is near 0%, no work gets done
- Most common: pressure too low or temperature differential too high

**AC makes room temperature worse:**
- Check if you accidentally have it in the wrong mode
- Heating mode when you need cooling (or vice versa)

---

## Gas Choice for Coolant Loops

Not all gases transfer heat equally:

| Gas | Thermal Performance | Notes |
|-----|---------------------|-------|
| **Pollutants (X)** | Excellent | Best heat transfer, common on Vulcan |
| **CO2** | Excellent | Best general-purpose choice |
| **Nitrogen (N2)** | Good | Works fine, readily available |
| **Oxygen (O2)** | Good | Don't waste your O2 on coolant |

For your waste loops, use CO2 or Pollutants when available. The faster heat transfer means better AC performance.

---

## Summary

**Portable AC (Beginner):**
- Connect to pipe network via Portables Connector BEFORE using
- The starter canister WILL explode without proper venting
- Good for emergency/temporary cooling with batteries

**Wall-Mounted AC (Intermediate):**
- Three efficiency factors: Pressure, Temperature Differential, Operational
- Keep room and waste loop above 100 kPa
- Keep temperature differential under 50°C per AC
- Use CO2 or Pollutants in waste loops
- Chain multiple ACs for large temperature changes

---

**Sources:**
- [Unofficial Stationeers Wiki - Portable Air Conditioner](https://stationeers-wiki.com/Portable_Air_Conditioner)
- [Unofficial Stationeers Wiki - Air Conditioner](https://stationeers-wiki.com/Air_Conditioner)
- [Unofficial Stationeers Wiki - Guide (Air Conditioner)](https://stationeers-wiki.com/Guide_(Air_Conditioner))
- [Unofficial Stationeers Wiki - Pipe Radiator](https://stationeers-wiki.com/Pipe_Radiator)
- [Unofficial Stationeers Wiki - Liquid Canister](https://stationeers-wiki.com/Liquid_Canister)
- [Steam Community Guide - AC Setups](https://steamcommunity.com/sharedfiles/filedetails/?id=3097106630)
- [Steam Community Discussions](https://steamcommunity.com/app/544550/discussions/)
