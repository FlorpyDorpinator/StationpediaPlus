# Game Mechanic: Smelting

A comprehensive explanation covering ores, alloys, super-alloys, required conditions, and safety procedures. Organized from simple to complex for new players.

**Data verified from:** Stationeers Wiki (stationeers-wiki.com) as of January 2025

---

## Section 1: Introduction & Furnace Types

Smelting is the gateway to advanced construction in Stationeers. Without it, you're stuck with basic materials. Understanding which furnace to use is your first decision.

### The Three Furnace Types

**Arc Furnace**
- Electric-powered, no combustion needed
- **Ores only** - cannot make alloys
- Perfect for bulk ingot production
- Best for: Iron, Gold, Copper, Silicon, Nickel, Cobalt, Lead ingots
- Use when: You need lots of basic ingots and have power to spare

**Furnace (Standard)**
- Combustion-based, requires fuel gas (H2 + O2 from ice)
- Makes steel and basic alloys
- This is what most players build first
- Makes: Steel, Solder, Electrum, Invar, Constantan
- Use when: You need alloys for construction and electronics

**Advanced Furnace**
- Required for super-alloys
- Higher pressure tolerance (max 60 MPa before explosion)
- Makes: Stellite, Inconel, Hastelloy, Astroloy, Waspaloy
- Use when: Building rockets or endgame equipment

### Key Concept

The furnace combines whatever's inside when temperature and pressure conditions are met. **YOU** must load the correct ratios before igniting. Ratios must be exact - 3:1 means exactly 3:1, not approximately.

### Safety Warning

All furnaces radiate heat into surrounding atmosphere. Your base WILL heat up. Plan for ventilation or isolate your smelting area.

---

## Section 2: Your First Smelt - Steel

Steel is every player's first alloy and the most common failure point. Master this before attempting anything else.

### Recipe (Verified)

| Component | Ratio | Example Quantities |
|-----------|-------|-------------------|
| Iron Ore | 3 | 12, 15, 75 |
| Coal Ore | 1 | 4, 5, 25 |

**Conditions:**
- Temperature: 900 K - 100 kK (wide range, easy to hit)
- Pressure: 1 - 100 MPa (wide range, easy to hit)

### What You Need

- **Furnace (Standard)** - not Arc Furnace!
- Iron and Coal in exact 3:1 ratio
- **2 Volatiles Ice + 1 Oxite Ice** (provides H2 fuel + O2 oxidizer)

### The Process

1. Load ores FIRST - Iron and Coal in 3:1 ratio (e.g., 75 Iron + 25 Coal)
2. Load ice - 2 Volatiles, 1 Oxite
3. Close the furnace
4. Activate (click igniter)
5. Wait - furnace will reach ~1050K and ~6.2 MPa
6. When "Output: Steel" shows and temp/pressure stabilize, open and collect

### Why It Works

- Volatiles ice releases H2 (hydrogen) - the fuel
- Oxite ice releases O2 (oxygen) - the oxidizer
- Combustion creates heat AND pressure simultaneously
- Steel's wide acceptable ranges (900K+, 1 MPa+) make it forgiving

### Common Failures

| Problem | Cause | Solution |
|---------|-------|----------|
| Nothing happened | Didn't ignite | Check the activation port, use welder or igniter |
| Got iron ingots instead | Pressure or temp too low | Add more ice |
| Furnace exploded | Too much ice | Standard furnace maxes at ~90 MPa |
| Ores disappeared, no output | Wrong ratio | Must be exactly 3:1 Iron:Coal |

---

## Section 3: Common Alloys

All use the Standard Furnace. **Critical:** These have NARROW operating windows unlike steel.

### Solder
*For electronics, circuits, cables*

| Component | Ratio |
|-----------|-------|
| Iron Ore | 1 |
| Lead Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 350 K | 550 K |
| Pressure | 1 MPa | 100 MPa |

**Warning:** Temperature has a MAX of 550K. Too hot = failure. This is the only alloy where you might need to COOL the furnace or use less ice.

### Electrum
*For advanced electronics, some sensors*

| Component | Ratio |
|-----------|-------|
| Silver Ore | 1 |
| Gold Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 600 K | 100 kK |
| Pressure | 0.8 MPa | 2.4 MPa |

**Warning:** Pressure has a narrow window (0.8-2.4 MPa). Too much ice will overshoot. Use minimal ice - 1 Volatiles + 1 Oxite or less.

### Invar
*For precision instruments, advanced frames*

| Component | Ratio |
|-----------|-------|
| Iron Ore | 1 |
| Nickel Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 1,200 K | 1,500 K |
| Pressure | 18 MPa | 20 MPa |

**Warning:** BOTH windows are narrow. Temperature must be 1.2-1.5 kK AND pressure must be 18-20 MPa. This is one of the hardest basic alloys. You need significant ice but precise amounts.

### Constantan
*For heating elements, thermocouples*

| Component | Ratio |
|-----------|-------|
| Copper Ore | 1 |
| Nickel Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 1,000 K | 10 kK |
| Pressure | 20 MPa | 100 MPa |

**Notes:** High minimum pressure (20 MPa) but wide ranges otherwise. Easier than Invar once you get enough pressure.

### Alloy Difficulty Ranking

1. **Steel** - Easiest (wide ranges)
2. **Solder** - Easy but watch max temp
3. **Constantan** - Medium (need high pressure)
4. **Electrum** - Tricky (narrow pressure window)
5. **Invar** - Hardest basic alloy (both windows narrow)

---

## Section 4: Super-Alloys

Super-alloys require the **Advanced Furnace** and have tight operating windows. These recipes use **common ores plus Steel ingots** - not exotic ores.

### Critical Rules for ALL Super-Alloys

1. **Use Advanced Furnace only** (60 MPa max capacity)
2. **Recipes use ingots AND ores** - some require Steel Ingots, not ores
3. **Monitor pressure/temp constantly** - windows can be very narrow
4. As of 2021, recipes return only **1 ingot per batch**

---

### Stellite
*High-wear applications, cutting tools*

| Component | Quantity |
|-----------|----------|
| Silver Ore | 3 |
| Cobalt Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 1,800 K | 100 kK |
| Pressure | 10 MPa | 20 MPa |

**Notes:** Most forgiving super-alloy. Wide temperature range. Good first attempt.

---

### Inconel
*Heat shields, rocket components*

| Component | Quantity |
|-----------|----------|
| Gold Ore | 2 |
| Nickel Ore | 1 |
| Steel Ingot | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 600 K | 100 kK |
| Pressure | 23.5 MPa | 24 MPa |

**Warning:** That 0.5 MPa pressure window is brutal. Requires precise pressure control. Monitor closely and vent small amounts to dial in.

---

### Hastelloy
*Corrosion-resistant components*

| Component | Quantity |
|-----------|----------|
| Silver Ore | 2 |
| Nickel Ore | 1 |
| Cobalt Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 950 K | 1,000 K |
| Pressure | 25 MPa | 30 MPa |

**Warning:** Temperature window is only 50K (950-1000K). Pressure window is reasonable at 5 MPa range.

---

### Astroloy
*Aerospace, high-stress applications*

| Component | Quantity |
|-----------|----------|
| Steel Ingot | 2 |
| Copper Ore | 1 |
| Cobalt Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 1,000 K | 100 kK |
| Pressure | 30 MPa | 40 MPa |

**Notes:** Requires Steel Ingots (smelt steel first). 10 MPa pressure window is workable. Wide temp range.

---

### Waspaloy
*Ultimate high-temp applications*

| Component | Quantity |
|-----------|----------|
| Lead Ore | 2 |
| Silver Ore | 1 |
| Nickel Ore | 1 |

| Condition | Min | Max |
|-----------|-----|-----|
| Temperature | 400 K | 800 K |
| Pressure | 50 MPa | 100 MPa |

**Notes:** Highest pressure requirement (50+ MPa) but widest pressure window. Temperature is counterintuitively LOW - don't overheat!

---

### Super-Alloy Difficulty Ranking

1. **Stellite** - Easiest (wide ranges, common ores)
2. **Astroloy** - Medium (needs Steel Ingots, reasonable windows)
3. **Waspaloy** - Medium (high pressure but wide windows)
4. **Hastelloy** - Hard (tight 50K temp window)
5. **Inconel** - Hardest (0.5 MPa pressure window!)

---

## Section 5: Safety & Troubleshooting

Furnaces will kill you. They'll explode, cook your base, or waste hours of ore mining. Here's how to survive.

### Explosion Prevention

| Furnace Type | Max Pressure | Consequence |
|--------------|--------------|-------------|
| Standard Furnace | ~90 MPa | Explodes, destroys contents |
| Advanced Furnace | 60 MPa | Explodes, destroys contents |

**Prevention Rules:**
- **Never overload ice** - 3 Volatiles + 2 Oxite is usually the safe max
- **Watch the pressure gauge** during smelting
- **Vent early** if pressure climbing dangerously

### Heat Management

Furnaces radiate heat into surrounding atmosphere. A 1000K furnace WILL cook your base and kill you.

**Solutions:**

1. **Isolate it** - Put furnace in separate room with airlock, vent to space after each smelt
2. **Vacuum smelting** - No atmosphere = no heat transfer to base
3. **Active cooling** - Wall coolers + radiators if you want it indoors
4. **Distance** - Further from living space = more time to react

### Troubleshooting Quick Reference

| Problem | Cause | Fix |
|---------|-------|-----|
| Nothing happens when ignited | No fuel gas or no O2 | Check ice loaded, try more Oxite |
| Got base ingots instead of alloy | Temp or pressure outside range | Check BOTH min AND max values |
| Wrong alloy came out | Wrong ore ratio | Ratios must be exact |
| Ores vanished, nothing output | Invalid ratio | Check recipe again |
| Furnace exploded | Overpressure | Use less ice |
| Base is heating up | Normal furnace behavior | Isolate or vacuum smelt |
| Alloy won't form despite correct values | Outside MAX temp/pressure | Some alloys have narrow windows - check maximums |

### Emergency Procedures

**Overheating base:**
- Vent atmosphere to space immediately
- Seal furnace room from living area

**Pressure climbing dangerously:**
- Open furnace to vent (loses heat too)
- Or use attached pipe to bleed off gas gradually

---

## Quick Reference Card

### Basic Alloys (Standard Furnace)

| Alloy | Recipe (Ratio) | Temp Range | Pressure Range |
|-------|----------------|------------|----------------|
| Steel | 3 Iron + 1 Coal | 900 K - 100 kK | 1 - 100 MPa |
| Solder | 1 Iron + 1 Lead | 350 - 550 K | 1 - 100 MPa |
| Electrum | 1 Silver + 1 Gold | 600 K - 100 kK | 0.8 - 2.4 MPa |
| Invar | 1 Iron + 1 Nickel | 1.2 - 1.5 kK | 18 - 20 MPa |
| Constantan | 1 Copper + 1 Nickel | 1 - 10 kK | 20 - 100 MPa |

### Super-Alloys (Advanced Furnace)

| Alloy | Recipe | Temp Range | Pressure Range |
|-------|--------|------------|----------------|
| Stellite | 3 Silver + 1 Cobalt | 1.8 - 100 kK | 10 - 20 MPa |
| Inconel | 2 Gold + 1 Nickel + 1 Steel | 600 K - 100 kK | 23.5 - 24 MPa |
| Hastelloy | 2 Silver + 1 Nickel + 1 Cobalt | 950 - 1000 K | 25 - 30 MPa |
| Astroloy | 2 Steel + 1 Copper + 1 Cobalt | 1 - 100 kK | 30 - 40 MPa |
| Waspaloy | 2 Lead + 1 Silver + 1 Nickel | 400 - 800 K | 50 - 100 MPa |

### Key Notes

- **Ratios must be exact** - 3:1 means exactly 3:1
- **Check BOTH min AND max** - Many alloys fail from exceeding maximums
- **Super-alloys return 1 ingot** per batch (as of 2021 update)
- **Inconel and Astroloy need Steel Ingots** - smelt steel first

---

*Data verified from Stationeers Wiki (January 2025). Game updates may change values - if a recipe fails, check the in-game Stationpedia (F1).*
