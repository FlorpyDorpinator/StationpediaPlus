# Species Health & Survival

A comprehensive informational covering the survival needs of all playable species: Humans, Zrilians, and Robots.

**Data verified from:** Decompiled game files (Lungs.cs, LungsZrilian.cs, Entity.cs, SpeciesClass.cs, Chemistry.cs)

---

## Section 1: Overview

Stationeers has three playable species, each with fundamentally different survival requirements:

| Species | Breathes | Toxic Gases | Temperature Range | Food/Water |
|---------|----------|-------------|-------------------|------------|
| **Human** | Oxygen (O2) | Volatiles + Pollutant | -10°C to +50°C | Yes |
| **Zrilian** | Volatiles (H2) | Pollutant only | -20°C to +80°C | Yes |
| **Robot** | None | None | Wider tolerance | No |

**Critical Implication:** Humans and Zrilians cannot share the same atmosphere! What one breathes is toxic to the other.

---

## Section 2: Human Survival

### Atmospheric Requirements (From Lungs.cs)

| Requirement | Value | Source |
|-------------|-------|--------|
| **Breathable Gas** | Oxygen (O2) | `BreathedType = Chemistry.GasType.Oxygen` |
| **Minimum O2 Pressure** | 16 kPa partial pressure | `MinimumOxygenPartialPressure = 16.0` |
| **Toxic Gases** | Volatiles + Pollutant | `ToxicTypes = GasType.Volatiles \| GasType.Pollutant` |
| **Toxin Warning** | 0.5 kPa partial pressure | `ToxicPartialPressureForWarning = 0.5` |
| **Toxin Damage** | 1.0 kPa partial pressure | `ToxicPartialPressureForDamage = 1.0` |

### Temperature Tolerance (From Lungs.cs)

| Limit | Temperature | Kelvin |
|-------|-------------|--------|
| **Minimum** | -10°C | 263 K |
| **Maximum** | +50°C | 323 K |

Calculation: `ZeroDegrees (273.15K) ± offset`
- Min: 273.15 - 10 = 263.15 K
- Max: 273.15 + 50 = 323.15 K

### Ideal Human Atmosphere

| Gas | Partial Pressure | Purpose |
|-----|------------------|---------|
| Oxygen (O2) | 20-25 kPa | Breathing (16 kPa minimum) |
| Nitrogen (N2) | 80 kPa | Filler (inert, safe) |
| **Total** | ~100 kPa | Earth-like pressure |

**Safe Ranges:**
- Temperature: 20-25°C (comfortable, healing occurs)
- Pressure: 50-200 kPa total (too high = suit warning)
- O2: 16+ kPa partial pressure
- Toxins: <0.5 kPa combined Volatiles + Pollutant

### Human Danger Signs

| Warning | Meaning | Action |
|---------|---------|--------|
| Low O2 icon | O2 < 16 kPa | Add oxygen, check tank/filters |
| Toxin icon | Volatiles or Pollutant > 0.5 kPa | Vent atmosphere, filter toxins |
| Cold damage | Temp < -10°C | Heat atmosphere, suit heater |
| Heat damage | Temp > 50°C | Cool atmosphere, move away |

---

## Section 3: Zrilian Survival

### Atmospheric Requirements (From LungsZrilian.cs)

| Requirement | Value | Source |
|-------------|-------|--------|
| **Breathable Gas** | Volatiles (H2) | `PartialPressureVolatiles` used for efficiency |
| **Minimum Volatiles Pressure** | 16 kPa partial pressure | Uses same threshold as human O2 |
| **Toxic Gas** | Pollutant ONLY | `ToxinLevel = PartialPressurePollutant` |
| **Toxin Warning** | 0.5 kPa partial pressure | Same as human |
| **Toxin Damage** | 1.0 kPa partial pressure | Same as human |

**Critical:** Volatiles (H2) are NOT toxic to Zrilians - they breathe it! But Volatiles ARE toxic to Humans.

### Temperature Tolerance (From LungsZrilian.cs)

| Limit | Temperature | Kelvin |
|-------|-------------|--------|
| **Minimum** | -20°C | 253 K |
| **Maximum** | +80°C | 353 K |

Zrilians have **wider temperature tolerance** than Humans:
- 10°C colder minimum (-20°C vs -10°C)
- 30°C hotter maximum (+80°C vs +50°C)

### Ideal Zrilian Atmosphere

| Gas | Partial Pressure | Purpose |
|-----|------------------|---------|
| Volatiles (H2) | 20-25 kPa | Breathing (16 kPa minimum) |
| Nitrogen (N2) | 80 kPa | Filler (inert, safe for Zrilians) |
| **Total** | ~100 kPa | Comfortable pressure |

**Safe Ranges:**
- Temperature: 20-40°C (wider comfort zone)
- Pressure: 50-200 kPa total
- Volatiles: 16+ kPa partial pressure
- Pollutant: <0.5 kPa

### Zrilian Danger Signs

| Warning | Meaning | Action |
|---------|---------|--------|
| Low O2 icon* | Volatiles < 16 kPa | Add volatiles (from ice) |
| Toxin icon | Pollutant > 0.5 kPa | Filter pollutant |
| Cold damage | Temp < -20°C | Heat atmosphere |
| Heat damage | Temp > 80°C | Cool atmosphere |

*The UI still shows the O2 icon, but Zrilians need Volatiles, not Oxygen.

---

## Section 4: Robot Survival

### Atmospheric Requirements

Robots have no breathing requirements:
- `IsArtificial` check bypasses nutrition and respiration
- No atmosphere needed for survival
- Can operate in vacuum indefinitely

### Temperature Tolerance

Robots have wider temperature tolerance than organic species, but still take damage from extreme temperatures. Exact limits depend on specific robot type.

### Power Requirements

Instead of food/water, robots need:
- Battery power (varies by robot type)
- Some robots can recharge from power networks

### Robot Advantages

- No atmosphere needed
- No food or water consumption
- Wider temperature tolerance
- Can work in vacuum
- No toxin concerns

### Robot Disadvantages

- Battery dependent
- Cannot eat/drink for quick healing
- May require specialized repair

---

## Section 5: Food & Hydration (Organic Species)

Both Humans and Zrilians have identical food and water requirements.

### Nutrition (From Entity.cs)

| Stat | Value | Source |
|------|-------|--------|
| **Max Nutrition** | 5 units (base) | `BaseNutritionStorage` returns 5f |
| **Full Nutrition** | 4.5 units | `FullNutrition = 4.5f` |
| **Warning Level** | 2 units | `WarningNutrition = 2f` |
| **Critical Level** | 1 unit | `CriticalNutrition = 1f` |

### Hydration (From Entity.cs)

| Stat | Value | Source |
|------|-------|--------|
| **Max Hydration** | 5 units | Hardcoded |
| **Warning Level** | 2 units | `WarningHydration = 2f` |
| **Critical Level** | 1 unit | `CriticalHydration = 1f` |

### Starvation & Dehydration Damage

When Nutrition or Hydration reaches 0:
- Damage accumulates over time
- Rate affected by difficulty setting
- Damage type: Starvation (nutrition) or Hydration (water)
- Healing occurs when fed/hydrated if damage < certain threshold

### Food Sources

Both species can eat the same foods:
- Canned food (starting supplies)
- Grown plants (tomatoes, corn, potatoes, etc.)
- Cooked meals (from plants + processing)
- Eggs, milk, meat (from animals)

### Water Sources

- Water bottles (starting supplies)
- Filled from water tanks
- Vending machines (if stocked)

---

## Section 6: Shared Atmosphere Challenges

### The Human-Zrilian Problem

**Humans need:** O2 (breathe) + no Volatiles (toxic)
**Zrilians need:** Volatiles (breathe) + no Pollutant (toxic)

These requirements are **mutually exclusive**:
- Volatiles are life support for Zrilians
- Volatiles are toxic to Humans
- They cannot share the same air

### Solutions for Multi-Species Bases

1. **Separate Habitats**
   - Human areas: O2 + N2 atmosphere
   - Zrilian areas: Volatiles + N2 atmosphere
   - Airlocks between zones

2. **Suit-Only Common Areas**
   - Shared work areas in vacuum
   - Everyone wears suits with appropriate tanks

3. **Rotating Shifts**
   - Flush atmosphere between species
   - Time-consuming but uses one habitat

### Suit Requirements

**Human Suits:**
- Tank with O2
- Filter for Volatiles and Pollutant

**Zrilian Suits:**
- Tank with Volatiles (H2)
- Filter for Pollutant

---

## Section 7: Quick Survival Checklist

### Human First Minutes

1. Check suit O2 tank pressure
2. Find/build shelter before suit runs out
3. Set up O2 supply (Oxite ice → furnace/electrolyzer)
4. Remove Volatiles from atmosphere (vent or filter)
5. Maintain temperature -10°C to +50°C
6. Eat and drink before critical

### Zrilian First Minutes

1. Check suit Volatiles tank pressure
2. Find/build shelter before suit runs out
3. Set up Volatiles supply (Volatiles ice → furnace/electrolyzer)
4. Remove Pollutant from atmosphere (filter)
5. Maintain temperature -20°C to +80°C
6. Eat and drink before critical

### Robot First Minutes

1. Check battery level
2. Locate power source for recharging
3. No atmosphere concerns
4. Operate indefinitely if powered

---

## Quick Reference Card

### Atmospheric Needs

| Species | Breathes | Min Pressure | Toxic |
|---------|----------|--------------|-------|
| Human | O2 | 16 kPa | Volatiles, Pollutant |
| Zrilian | Volatiles | 16 kPa | Pollutant |
| Robot | None | N/A | None |

### Temperature Ranges

| Species | Min | Max |
|---------|-----|-----|
| Human | -10°C (263K) | +50°C (323K) |
| Zrilian | -20°C (253K) | +80°C (353K) |
| Robot | Wider tolerance | Varies |

### Toxin Thresholds

| Level | Partial Pressure | Effect |
|-------|------------------|--------|
| Warning | 0.5 kPa | UI warning |
| Damage | 1.0 kPa | Health damage |

### Food/Water (Organics)

| Stat | Max | Warning | Critical |
|------|-----|---------|----------|
| Nutrition | 5 | 2 | 1 |
| Hydration | 5 | 2 | 1 |

### Gas Sources

| Gas | Ice Source | Used By |
|-----|------------|---------|
| Oxygen (O2) | Oxite | Humans |
| Volatiles (H2) | Volatiles | Zrilians |
| Nitrogen (N2) | Nitrice | Both (filler) |

---

*Data extracted from decompiled game files: Lungs.cs, LungsZrilian.cs, Entity.cs, SpeciesClass.cs, Chemistry.cs. Values are authoritative as of game version current to January 2026.*
