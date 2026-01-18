# Complete Stirling Engine Guide for Stationeers

A comprehensive guide covering the Stirling Engine power generator, including setup, efficiency optimization, and IC10 monitoring.

**Data verified from:** Decompiled game files (StirlingEngine.cs)

---

## Section 1: Introduction & Overview

The Stirling Engine is a heat-differential power generator. It converts temperature differences between a hot gas input and the cold room atmosphere into electrical power. Unlike solar panels or solid fuel generators, it requires continuous heat input but can run indefinitely with the right setup.

### Key Specifications (From Game Code)

| Spec | Value | Source |
|------|-------|--------|
| **Max Power Output** | 6,000W (6 kW) | `maxPower = 6000f` |
| Piston Volume | 4 L | `PistonVolume = 4f` |
| Internal (Regenerator) Volume | 56 L | `InternalVolume = 56f` |
| Hot Side Heat Exchanger Area | 3 m² | `HotSideHeatExchangerArea = 3f` |
| Cold Side Heat Exchanger Area | 2 m² | `ColdSideHeatExchangerArea = 2f` |
| Heat Exchanger Atmosphere Volume | 10 L | `HeatExchangerAtmosphereVolume = 10f` |
| **Ideal Pressure Differential** | 3,000 kPa (3 MPa) | `IdealPressureDifferential = 3000f` |
| Max Operating Temperature | 3,000 K | `MAXOperatingTemperature = 3000K` |
| Max RPM (visual only) | 300 | `MaxRpm = 300f` |

### How It Works

The Stirling Engine uses the temperature difference between:
1. **Hot Side:** Connected to input pipe (receives hot gas)
2. **Cold Side:** Exposed to room atmosphere

The greater the temperature difference, the more power generated. A sealed gas canister provides the working fluid that transfers energy between the hot and cold chambers.

### What You Need

1. **Gas Canister** - Any gas canister (closed, not broken)
2. **Hot Input Pipe** - Connected to hot gas source (furnace exhaust, hot atmosphere)
3. **Output Pipe** - Returns cooled gas to the loop
4. **Cold Room** - The engine's cold side needs exposure to atmosphere

---

## Section 2: Setup Requirements

### Placement Requirements

- **Must be placed on a frame** (verified in CanConstruct)
- Cold side (fan area) must be exposed to room atmosphere
- Cannot be fully enclosed - needs atmospheric access

### Connection Requirements (From IsAbleToOperate)

| Requirement | Condition |
|-------------|-----------|
| Input Network | Valid pipe connection to hot side |
| Output Network | Valid pipe connection for return |
| Gas Canister | Inserted, closed, and not broken |

**Error State:** If any requirement is not met, the engine enters Error state and outputs 0W.

### Gas Canister Rules

- The canister must be **closed** (`!GasCanister.IsOpen`)
- The canister must be **intact** (`!GasCanister.IsBroken`)
- When turned on, the canister's gas is moved into the internal atmosphere
- When turned off, the gas is returned to the canister

**Tip:** Pre-pressurize your canister with a gas that has high thermal efficiency (like Nitrogen) for better performance.

---

## Section 3: Efficiency System

The Stirling Engine's output is calculated by multiplying three efficiency factors:

### Power Calculation Formula (From OnAtmosphericTick)

```
Power Output = Min(MaxPower, EnergyDifference × WorkingGasEfficiency × EnvironmentEfficiency × PressureDifferentialEfficiency)
```

Where:
- **EnergyDifference** = |HotSideEnergy - ColdSideEnergy|
- **MaxPower** = 6,000W (hard cap)

### Efficiency Factor 1: Working Gas Efficiency

| Factor | Source | Description |
|--------|--------|-------------|
| WorkingGasEfficiency | `InternalAtmosphere.ThermalEfficiency()` | Based on gas type in canister |

Different gases have different thermal efficiencies. The gas in your canister determines this factor.

| Gas | Approximate Efficiency |
|-----|----------------------|
| Nitrogen (N2) | ~0.12 (good) |
| Oxygen (O2) | ~0.10 |
| Carbon Dioxide (CO2) | ~0.08 |
| Volatiles (H2) | ~0.14 (higher) |

**Note:** Thermal efficiency is a property of the gas mixture. Pure gases work best.

### Efficiency Factor 2: Environment Efficiency

| Factor | Source | Description |
|--------|--------|-------------|
| EnvironmentEfficiency | `MachineEfficiency.Evaluate(roomTemperature)` | Based on room atmosphere temperature |

The default curve returns 1.0 (100%) for all temperatures from 0K to 3000K, meaning room temperature typically doesn't penalize efficiency.

**Cold rooms = Better cooling = More power**

### Efficiency Factor 3: Pressure Differential Efficiency

| Factor | Source | Description |
|--------|--------|-------------|
| PressureDifferentialEfficiency | `(HotPressure - ColdPressure) / IdealPressureDifferential` | Clamped 0-1 |

This is the **most important factor** to optimize:

- **Ideal Pressure Differential:** 3,000 kPa (3 MPa)
- Calculated as: (Hot Side Pressure - Cold Side Pressure) / 3000
- Clamped to 0-100%

**To maximize this:**
1. Increase hot side pressure (hot gas = higher pressure)
2. Keep cold side as cool as possible (colder = lower pressure)
3. Use higher pressure input gas

---

## Section 4: Optimal Setup

### Maximizing Power Output

| Priority | Action | Why |
|----------|--------|-----|
| 1 | Large temperature differential | More energy to extract |
| 2 | High pressure hot gas | Higher pressure differential efficiency |
| 3 | Cold room atmosphere | Better heat rejection |
| 4 | Good thermal efficiency gas | Better energy transfer |

### Hot Side Heat Sources

| Source | Temperature | Notes |
|--------|-------------|-------|
| Furnace exhaust | 500-2000K+ | Direct pipe connection |
| Rocket exhaust | Very high | Challenging to capture |
| H2 combustion | 2000K+ | Requires combustion chamber |
| Solar heater | Planet dependent | Passive but limited |

### Example Piping Setup

```
[Hot Gas Source] → [Input Pipe] → [STIRLING ENGINE] → [Output Pipe] → [Cooling Loop/Return]
                                        ↓
                                   [Room Atmosphere]
                                   (Cold Side Exhaust)
```

### Multi-Engine Configuration

Multiple engines can share the same hot gas loop:

```
[Furnace] → [Hot Gas Pipe] ─┬→ [Engine 1] → [Return Pipe] ─┐
                            ├→ [Engine 2] → [Return Pipe] ─┤
                            └→ [Engine 3] → [Return Pipe] ─┘
                                                           ↓
                                                      [Cooling]
```

**Note:** Each engine extracts heat from the gas, so downstream engines receive progressively cooler gas.

---

## Section 5: Logic Variables

### Readable Variables (From GetLogicValue)

| Variable | Type | Description |
|----------|------|-------------|
| **PowerGeneration** | float | Current power output (W) |
| **Pressure** | float | Internal atmosphere pressure (kPa) |
| **Temperature** | float | Internal atmosphere temperature (K) |
| **Quantity** | float | Total moles (internal + hot side + cold side) |
| **EnvironmentEfficiency** | float | Room-based efficiency (0-1) |
| **WorkingGasEfficiency** | float | Gas-based efficiency (0-1) |
| RatioOxygen | float | O2 ratio in internal atmosphere |
| RatioCarbonDioxide | float | CO2 ratio |
| RatioNitrogen | float | N2 ratio |
| RatioPollutant | float | X ratio |
| RatioVolatiles | float | H2 ratio |
| RatioWater | float | H2O ratio |
| RatioNitrousOxide | float | N2O ratio |
| Combustion | int | 1 if sparked, 0 otherwise |

### Note on Pressure Differential

The `MachinePressureDifferentialEfficiency` is calculated internally but not directly readable via logic. However, you can estimate it if you know your hot/cold side conditions:

```
PressureDiffEfficiency = (HotPressure - ColdPressure) / 3000
```

---

## Section 6: IC10 Monitoring

### Basic Power Monitor

```mips
alias Engine d0
alias Display d1

main:
    # Read current power output
    l r0 Engine PowerGeneration

    # Display power in kW
    div r0 r0 1000
    s Display Setting r0

    yield
    j main
```

### Efficiency Monitor

```mips
alias Engine d0
alias MemEnvEff d1      # Memory for environment efficiency
alias MemGasEff d2      # Memory for gas efficiency

main:
    # Read efficiencies
    l r0 Engine EnvironmentEfficiency
    l r1 Engine WorkingGasEfficiency

    # Store in memory (multiply by 100 for percentage display)
    mul r0 r0 100
    mul r1 r1 100
    s MemEnvEff Setting r0
    s MemGasEff Setting r1

    yield
    j main
```

### Overpressure Warning System

```mips
alias Engine d0
alias Alarm d1

define MAX_PRESSURE 50000  # 50 MPa warning threshold

main:
    l r0 Engine Pressure
    sgt r1 r0 MAX_PRESSURE
    s Alarm On r1

    yield
    j main
```

### Multi-Engine Power Total

```mips
define EngineHash HASH("StructureStirlingEngine")

main:
    # Sum power from all engines on network
    lb r0 EngineHash PowerGeneration Sum

    # r0 now contains total power from all Stirling Engines
    # Display or use as needed

    yield
    j main
```

---

## Section 7: Explosion & Safety

### Overpressure Danger (From HandlePressureCheck)

The Stirling Engine can **explode** if internal pressure exceeds its MaxPressureDelta rating:

| Parameter | Value |
|-----------|-------|
| Explosion Force | 1850 |
| Explosion Radius | 2 meters |
| Max Explosion Radius | 4 meters |

### Explosion Conditions

1. Internal pressure significantly exceeds MaxPressureDelta
2. Random roll based on pressure difference
3. Engine takes 200 brute damage per explosion event

### Warning Signs

In the tooltip display:
- Pressure text turns **red** when above 80% of MaxPressureDelta
- Engine plays "stressed" sound when overpressured

### Prevention

1. Don't overfill the gas canister
2. Monitor internal pressure via logic
3. Use pressure relief systems if needed
4. Keep cold side well-ventilated

---

## Section 8: Troubleshooting

### Not Generating Power

| Problem | Cause | Solution |
|---------|-------|----------|
| Error state | Missing requirement | Check canister, input pipe, output pipe |
| 0W output | No temperature difference | Verify hot gas is actually hot |
| 0W output | Canister empty | Use pre-filled canister |
| 0W output | Canister open | Close the canister before starting |
| Low output | Low pressure differential | Increase hot side pressure |

### Error States

| Error | Meaning | Fix |
|-------|---------|-----|
| Error = 1 | Missing canister or broken canister | Insert working canister |
| Error = 1 | Canister is open | Close canister |
| Error = 1 | Input not connected | Connect input pipe |
| Error = 1 | Output not connected | Connect output pipe |

### Low Efficiency

| Symptom | Cause | Solution |
|---------|-------|----------|
| Low WorkingGasEfficiency | Poor gas choice | Use nitrogen or volatiles |
| Low EnvironmentEfficiency | Very hot room | Cool the room |
| Low PressureDifferential | Not enough pressure difference | Increase hot gas pressure/temperature |

### Gas Loss

When the engine is turned **OFF**, internal gas returns to the canister. When turned **ON**, canister gas moves to internal atmosphere.

If canister is missing/broken when turning off, gas vents to room atmosphere.

---

## Section 9: Setup Checklist

### Initial Setup

1. [ ] Place Stirling Engine on frame
2. [ ] Connect input pipe to hot gas source
3. [ ] Connect output pipe (for gas return loop)
4. [ ] Ensure cold side exposed to room atmosphere
5. [ ] Insert gas canister (filled, closed)
6. [ ] Connect to power network (cable)
7. [ ] Turn on

### Verification

1. [ ] Error indicator = 0 (no error)
2. [ ] PowerGeneration > 0
3. [ ] Hot side temperature significantly higher than cold side
4. [ ] Internal pressure not approaching danger levels

### Optimization

1. [ ] Check WorkingGasEfficiency via logic
2. [ ] Check EnvironmentEfficiency via logic
3. [ ] Calculate estimated PressureDifferentialEfficiency
4. [ ] Adjust hot gas temperature/pressure for maximum output

---

## Quick Reference Card

### Specifications

| Spec | Value |
|------|-------|
| Max Power | 6,000W |
| Ideal Pressure Diff | 3 MPa (3000 kPa) |
| Max Operating Temp | 3000K |
| Piston Volume | 4L |
| Internal Volume | 56L |

### Efficiency Formula

```
Power = Min(6000W, EnergyDiff × GasEff × EnvEff × PressEff)
```

### Requirements

- Gas canister (closed, intact)
- Input pipe (hot gas)
- Output pipe (return)
- Room atmosphere exposure (cold side)

### Key Logic Variables

| Read | Description |
|------|-------------|
| PowerGeneration | Current output (W) |
| Pressure | Internal pressure (kPa) |
| Temperature | Internal temp (K) |
| EnvironmentEfficiency | 0-1 |
| WorkingGasEfficiency | 0-1 |

### Safety

- Monitor pressure (explodes if overpressured)
- Pressure warning at 80% of MaxPressureDelta
- Keep room ventilated for cold side

---

*Data extracted from decompiled game file: StirlingEngine.cs. Values are authoritative as of game version current to January 2026.*
