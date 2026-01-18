# Top 20 Community Questions - Stationeers

Generated from Discord (#game-help, #atmospherical-chitchat, #logic-circuit-discussion, etc.) and Steam Discussions on 2025-12-30.

These represent the most frequently asked questions and pain points for Stationeers players, prioritized for Stationpedia Ascended content development.

---

## Getting Started / Early Game

### 1. How do I smelt steel?
- Temperature/pressure requirements (900K+, up to 100MPa)
- Using ices (8 volatile + 4 oxite typical)
- First steel strategies on different planets (Europa cold, Vulcan/Venus heat)
- Iron:Coal ratio (3:1 or 15:5 for standard batch)

### 2. How do I survive the first few days?
- Early game strategy and priorities
- Basic sealed room setup
- Avoiding common death causes (suffocation, temperature)

### 3. How do I set up an airlock?
- Normal airlock (expects vacuum outside) vs Advanced airlock (atmosphere outside)
- Mars atmosphere causing issues with normal airlock
- Getting stuck when power dies - dedicated APC backup recommended
- Speeding up airlocks (pipe volume, pressure settings)

---

## Atmospherics & Temperature

### 4. How do I cool/heat my base?
- Radiators only work via radiation in vacuum; convection dominates in atmosphere
- AC units and their quirks (input bottlenecking exploit)
- Heat exchangers for transferring heat between pipe networks
- Wall coolers/heaters for room temperature

### 5. Why is my pressure/temperature wrong?
- Pipe network management
- Regulator flow requirements (need pressure advantage)
- Condensation/freezing in pipes (phase diagrams)
- "Nil" readings mean vacuum

### 6. How do pressure regulators work?
- Pressure Regulator: limits OUTPUT pressure
- Back Pressure Regulator: limits INPUT pressure (maintains upstream)
- Need larger pipe networks on both sides for good flow
- Common use: canister filling, room pressure control

### 7. How do I get rid of N2O/pollutants?
- Filtration unit to separate gases
- Condensation at low temperatures
- Centrifuge for liquid separation
- Venting to space (wasteful but simple)

---

## Power Systems

### 8. How do I make solar panels track the sun?
- Requires IC10 + Daylight Sensor
- Panel orientation matters for scripts
- Common script reads Horizontal/Vertical from sensor, writes to panels
- Panels need power to move - chicken/egg problem with empty batteries

### 9. Why aren't my batteries charging/discharging?
- Check cable connections and network
- APC vs direct connection
- Transformer for stepping down voltage
- Solar panels producing but batteries not filling = check load

### 10. What's the best early power setup?
- Solid fuel generator for immediate power (coal)
- Basic solar panels (no tracking) for passive income
- Station batteries for storage
- Transition to tracking solar panels after steel

---

## Automation & Logic

### 11. How does IC10/MIPS work?
- Assembly-like programming language
- 6 device pins (d0-d5) + 2 batch (db)
- 18 registers (r0-r15, sp, ra)
- Common starter: solar tracking, generator on/off based on battery

### 12. When should I use logic chips vs IC10?
- Logic chips: simple single-purpose tasks (read value, write value)
- IC10: complex multi-step logic, loops, conditions
- Chips are easier for beginners, IC10 more powerful

### 13. How do I automate [X]?
Common automation patterns:
- AC on/off based on room temperature
- Generator on/off based on battery charge
- Vent control based on pressure
- Airlock sequences

---

## Smelting & Production

### 14. Why won't my furnace activate/smelt?
- Check ore ratios (specific recipes in Stationpedia)
- Temperature must be in valid range
- Pressure must be in valid range
- Furnace needs to be closed and activated

### 15. How do I smelt super alloys (hastelloy, etc)?
- Very tight temperature tolerances (50C window for hastelloy)
- Gas management critical - ore releases gas that changes conditions
- Pre-heat/pre-pressurize before adding ore
- Advanced furnace recommended

### 16. Arc furnace vs advanced furnace?
- Arc furnace: uses electricity to heat, simpler
- Advanced furnace: uses gas for temperature/pressure control
- Arc furnace good for basic metals
- Advanced furnace needed for super alloys

---

## Plants & Food

### 17. How do I grow plants?
- Light cycle: ~14 hours on, ~6 hours off (or sync with daylight sensor)
- CO2 required (10% ratio typical)
- Water via pipe to hydroponics
- Check Stationpedia for species-specific requirements

### 18. Why are my plants growing slowly/dying?
- Temperature affects growth (both AIR and WATER temperature)
- Outside ideal range = slower growth
- Too far outside range = death
- Genetics drift toward current conditions over generations

---

## General Mechanics

### 19. How do I rotate/place items correctly?
- R key to rotate in hand
- T key for placement mode
- Scroll wheel or R during placement to rotate
- Some items have specific orientation requirements

### 20. How do I fill/refill canisters?
- Use Portable Tank + Tank Connector
- Or direct pipe connection with valve control
- Watch pressure limits (canisters have max pressure)
- Temperature affects pressure (hot gas = higher pressure)

---

## Bonus Common Issues

- **Multiplayer/P2P connection problems**: Dedicated server recommended
- **Getting stuck in unpowered airlocks**: Always have backup APC
- **Active vent overwhelming pipes**: Set PressureInternal lower
- **"Nil" readings**: Means vacuum - no temperature/pressure to measure
- **Radiators not cooling**: Check if you're in vacuum (radiation) or atmosphere (convection)

---

## Priority for Stationpedia Ascended

Suggested order for adding operationalDetails:

**High Priority (Beginner Pain Points):**
1. Furnace (steel smelting)
2. Airlock / Advanced Airlock
3. Solar Panel (tracking)
4. Pressure Regulator / Back Pressure Regulator
5. Active Vent
6. Station Battery / APC

**Medium Priority (Early-Mid Game):**
7. AC Unit
8. Radiators (all types)
9. Hydroponics Tray
10. Filtration Unit
11. IC Housing

**Lower Priority (Advanced):**
12. Advanced Furnace
13. Centrifuge
14. Heat Exchanger
15. Transformers
