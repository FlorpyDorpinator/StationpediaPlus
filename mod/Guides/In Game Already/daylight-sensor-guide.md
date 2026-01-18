````markdown
# Complete Daylight Sensor Guide for Stationeers

A comprehensive guide covering the Daylight Sensor, including solar tracking, logic integration, and automation setups.

**Data verified from:** Decompiled game files (DaylightSensor.cs, SolarPanel.cs)

## Quick Reference Card

### Modes

| Mode | Value | SolarAngle Output |
|------|-------|-------------------|
| Default | 0 | Sensor-to-sun angle |
| Horizontal | 1 | Sun azimuth |
| Vertical | 2 | Sun elevation |

### Logic Variables

| Variable | Description |
|----------|-------------|
| Horizontal | Sun azimuth (0-360°) |
| Vertical | Sun elevation (degrees) |
| SolarAngle | Mode-dependent angle |
| SolarIrradiance | Solar watts (0 if blocked) |
| Activate | 1 = has light, 0 = no light |

### Solar Panel Variables

| Variable | R/W | Description |
|----------|-----|-------------|
| Horizontal | R/W | Azimuth (degrees) |
| Vertical | R/W | Elevation (15-165°) |
| HorizontalRatio | R/W | Normalized (0-1) |
| VerticalRatio | R/W | Normalized (0-1) |

### Basic Tracking Code

```mips
alias Sensor d0
alias Panel d1

main:
    l r0 Sensor Horizontal
    l r1 Sensor Vertical
    s Panel Horizontal r0
    s Panel Vertical r1
    yield
    j main
```

### Batch Tracking Code

```mips
alias Sensor d0
define PanelHash HASH("StructureSolarPanelDual")

main:
    l r0 Sensor Horizontal
    l r1 Sensor Vertical
    sb PanelHash Horizontal r0
    sb PanelHash Vertical r1
    yield
    j main
```


## Section 1: Introduction & Overview

The Daylight Sensor detects sunlight and provides sun position data for automating solar panels, lights, and other systems. It's the key component for efficient solar power generation.

### Key Specifications (From Game Code)

| Spec | Value |
|------|-------|
| Power Consumption | 10W (inherits from SmallDevice) |
| Modes | 3 (Default, Horizontal, Vertical) |
| Trigger Output | HasLight (binary) |
| Angle Outputs | Horizontal, Vertical, SolarAngle (degrees) |

### What It Does

- **Detects sunlight** - `HasLight` property indicates if sensor has line-of-sight to sun
- **Outputs sun position** - Horizontal and Vertical angles in degrees
- **Measures solar intensity** - `SolarIrradiance` in watts (affected by weather)
- **Triggers circuitboards** - Can directly control linked motherboards

---

## Section 2: Modes

The sensor has three modes that change what `SolarAngle` outputs. Cycle modes by using logic/IC10 to change the Mode's value.

### Mode Reference (From DaylightSensorMode enum)

| Mode | Value | SolarAngle Output |
|------|-------|-------------------|
| Default | 0 | Angle between sensor's forward direction and sun |
| Horizontal | 1 | Sun's horizontal angle only (azimuth) |
| Vertical | 2 | Sun's vertical angle only (elevation) |

### Mode Details

**Default Mode (0):**
- Outputs angle between where the sensor is pointing and where the sun is
- Useful for detecting if sun is in front of the sensor
- Good for: Simple day/night detection, directional triggers

**Horizontal Mode (1):**
- Outputs only the sun's horizontal (azimuth) angle
- Range: 0-360 degrees
- Good for: Single-axis horizontal tracking

**Vertical Mode (2):**
- Outputs only the sun's vertical (elevation) angle
- Range: Depends on planet's sun arc
- Good for: Single-axis vertical tracking

**Note:** For solar panel tracking, you typically use the `Horizontal` and `Vertical` logic outputs directly, not the mode-dependent `SolarAngle`.

---

## Section 3: Logic Variables

### Readable Variables (From GetLogicValue)

| Variable | Type | Description |
|----------|------|-------------|
| **Horizontal** | float | Sun's horizontal angle (degrees, 0-360) |
| **Vertical** | float | Sun's vertical angle (degrees) |
| **SolarAngle** | float | Mode-dependent angle (degrees) |
| **SolarIrradiance** | float | Solar power in watts (0 if no light) |
| **Activate** | int | 1 if has sunlight, 0 if in shadow |

### How Angles Are Calculated (From OnThreadUpdate)

```
1. Get sun direction relative to sensor orientation
2. Convert to spherical coordinates
3. Horizontal = azimuth angle (0-360°)
4. Vertical = elevation angle
```

The angles are calculated relative to the sensor's mounting orientation. This is critical for solar tracking.

### SolarIrradiance Calculation (From LocalSolarIrradiance)

SolarIrradiance is calculated as follows: it is 0 when `HasLight` is false; otherwise
SolarIrradiance = OrbitalSimulation.SolarIrradiance × WeatherEvent.SolarRatio.

To sum it up:
- Returns 0 if sensor is in shadow
- Multiplied by weather's solar ratio (storms reduce output)
- Base value depends on planet distance from sun

---

## Section 4: Solar Panel Tracking

### Understanding Panel Angles

Solar panels have their own Horizontal and Vertical properties:

| Panel Property | Range | Description |
|----------------|-------|-------------|
| Horizontal | 0-360° | Azimuth rotation |
| Vertical | 15-165° | Elevation tilt (via ratio 0-1) |
| HorizontalRatio | 0-1 | Normalized horizontal |
| VerticalRatio | 0-1 | Normalized vertical |

**Important:** Panels accept degrees for Horizontal/Vertical, but internally use ratios.

### The Orientation Problem

**Critical:** The daylight sensor and solar panel must have matching orientations for direct angle copying to work.

**Same Orientation:**
- Both mounted on same surface type (both on ground, both on wall)
- Both facing the same compass direction
- Angles can be copied directly

**Different Orientation:**
- Angles will be offset
- Need Math Unit to correct, OR
- Physically rotate sensor to match panel orientation

### Basic Tracking Setup

**Hardware:**
- 1x Daylight Sensor
- 1x Solar Panel (with data port variant)
- 2x Logic Writer (or 2x Batch Writer for multiple panels)
- Cable Coil (data network)

**Wiring:**
```
[Daylight Sensor] ←data→ [Logic Writer #1] ←data→ [Solar Panel]
                         [Logic Writer #2] ←data→ [Solar Panel]
```

### Logic Writer Configuration

**Writer #1 (Horizontal):**
- Input Device: Daylight Sensor
- Input Variable: Horizontal
- Output Device: Solar Panel
- Output Variable: Horizontal

**Writer #2 (Vertical):**
- Input Device: Daylight Sensor
- Input Variable: Vertical
- Output Device: Solar Panel
- Output Variable: Vertical

### IC10 Direct Tracking

```mips
alias Sensor d0
alias Panel d1

main:
    # Read sun position from sensor
    l r0 Sensor Horizontal
    l r1 Sensor Vertical

    # Write to panel
    s Panel Horizontal r0
    s Panel Vertical r1

    yield
    j main
```

### IC10 Batch Tracking (Multiple Panels)

```mips
alias Sensor d0
define PanelHash HASH("StructureSolarPanelDual")

main:
    # Read sun position
    l r0 Sensor Horizontal
    l r1 Sensor Vertical

    # Write to ALL solar panels on network
    sb PanelHash Horizontal r0
    sb PanelHash Vertical r1

    yield
    j main
```

**Note:** Replace `HASH("StructureSolarPanelDual")` with whichever panel you are using. Check in-game with tablet or the Stationpedia page.

---

## Section 5: Advanced Setups

### Offset Correction (Mismatched Orientations)

If your sensor and panels have different orientations, add/subtract offsets:

```mips
alias Sensor d0
alias Panel d1

# Offset values - adjust these for your setup
define H_OFFSET 90   # Add 90° to horizontal
define V_OFFSET 0    # No vertical offset

main:
    l r0 Sensor Horizontal
    l r1 Sensor Vertical

    # Apply offsets
    add r0 r0 H_OFFSET
    mod r0 r0 360      # Wrap to 0-360
    add r1 r1 V_OFFSET

    s Panel Horizontal r0
    s Panel Vertical r1

    yield
    j main
```

Note: If you have a hard time getting this to work, just keep trying different offsets and eventually one will work.

### Weather-Aware Power Management

Use SolarIrradiance to detect storms and manage power:

```mips
alias Sensor d0
alias Battery d1
alias Generator d2

define LOW_IRRADIANCE 100   # Below this = storm/night
define BATTERY_LOW 0.3      # 30% battery threshold

main:
    # Check solar irradiance
    l r0 Sensor SolarIrradiance
    bgt r0 LOW_IRRADIANCE solarOk

    # Low solar - check battery
    l r1 Battery Ratio
    bgt r1 BATTERY_LOW batteryOk

    # Battery low + no solar = start generator
    s Generator On 1
    j done

solarOk:
batteryOk:
    s Generator On 0

done:
    yield
    j main
```

### Day/Night Light Control

```mips
alias Sensor d0
define LightHash HASH("StructureWallLight")

main:
    # Check if sun is up
    l r0 Sensor Activate

    # Invert for lights (on at night)
    seqz r0 r0

    # Set all lights
    sb LightHash On r0

    sleep 5    # Don't need to check every tick
    j main
```

---

## Section 6: Placement Guidelines

### Optimal Sensor Placement

1. **Unobstructed view** - Sensor needs line-of-sight to sun
2. **Match panel orientation** - Easiest if sensor faces same direction as panels
3. **Accessible for wiring** - Data port needs cable connection
4. **Protected from damage** - Consider mounting location. Non-heavy solar panels take damage in storms.

### Common Placement Mistakes

| Mistake | Problem | Solution |
|---------|---------|----------|
| Sensor inside | No sunlight detection | Mount outside |
| Behind structure | Blocked view | Move to clear location |
| Wrong orientation | Angle offsets needed | Rotate to match panels |
| Too far from panels | Long cable runs | Place near panel array |

### Sensor Orientation Reference

The sensor's **forward direction** is the arrow/face side. When mounting:

- **On ground:** Forward points along surface
- **On wall:** Forward points away from wall
- **On ceiling:** Forward points along ceiling surface

For direct tracking without offsets, mount sensor so its forward direction matches how panels would point if aimed at horizon at noon.

---

## Section 7: Troubleshooting

### No Sunlight Detection

| Problem | Cause | Solution |
|---------|-------|----------|
| Activate = 0 during day | Sensor blocked | Check for obstructions |
| Activate = 0 during day | Sensor inside | Move outside |
| Activate = 0 during day | Night time | Wait for sunrise |
| SolarIrradiance = 0 | Storm weather | Wait for clear weather |

### Tracking Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Panels aim wrong direction | Orientation mismatch | Add angle offsets |
| Panels oscillate | Update too fast | Add yield/sleep |
| Panels don't move | No data connection | Check cables |
| Panels don't move | Writers not configured | Check writer settings |
| Only horizontal works | Vertical not connected | Add second writer |
| Panel alignment wrong (opposite, only pointing down) | Daylight sensor and panels alignment doesn't match | Reposition sensor / add angle offset |
### Logic Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Can't read Horizontal | Using wrong variable | Use LogicType.Horizontal |
| Values seem wrong | Wrong mode | Mode affects SolarAngle only |
| Batch write not working | Wrong hash | Verify panel prefab hash |

---

## Section 8: Integration Examples

### With Logic Memory

Store sunrise/sunset times:

```mips
alias Sensor d0
alias Memory d1

# Track state
# r10 = was sun up last tick

main:
    l r0 Sensor Activate

    # Detect sunrise (was 0, now 1)
    beq r0 r10 noChange
    bgtz r0 sunrise
    j sunset

sunrise:
    # Store current game time as sunrise
    l r1 db Time
    s Memory Setting r1
    j noChange

sunset:
    # Could store sunset time in second memory

noChange:
    move r10 r0
    yield
    j main
```



### Power Budget Calculation

```
Per tracked panel array:
- Daylight Sensor: 10W
- Logic Writer x2: 10W each = 20W
- Total overhead: 30W

With Batch Writer:
- Daylight Sensor: 10W
- Batch Writer: 10W
- Total overhead: 20W (regardless of panel count)
```

---


---

*Data extracted from decompiled game files: DaylightSensor.cs, SolarPanel.cs, Sensor.cs. Values are authoritative as of game version current to January 2026.*

````