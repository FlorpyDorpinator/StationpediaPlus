# Part 3: Not Dying Tonight (Power, Temperature, and Atmosphere)

---

## The Three Killers

You have a sealed shelter. Now you need to survive inside it. Three things will kill you if you get them wrong:

1. **No Power** - Your doors won't open, your equipment won't run
2. **Wrong Temperature** - Too hot or cold and you'll take damage
3. **Bad Atmosphere** - Wrong pressure, wrong gases, or no oxygen

Let's solve each one.

---

## Power Systems

Everything in your base needs electricity. No power = no survival.

### Understanding Power Basics

**Cables carry electricity:**
- **Regular Cable** - Handles up to **5,000 Watts (5kW)**
- **Heavy Cable** - Handles up to **100,000 Watts (100kW)**

If you pull more power than a cable can handle, **the cable burns out**. This is a common beginner mistake. We recommend setting up your power structure like this:

Power Source (generator, solar)-->Transformer-->Battery-->APC-->Transformer-->Fuse-->Network. Please see the "Power" guide for more detail.69

**Your starter kit uses regular cable coils (50 in your toolbelt).** This means your entire early base is limited to 5kW total unless you make heavy cables later.

### Solar Panels

Your most automatable and simple power source. One **Kit (Solar Panel Basic)** is in your starter crate. This is the basic solar panel that can't adjust to the angle of the sun. You can plop it down on a frame and hook it up to your power distribution network. It isn't much good until you have a station battery to store all the power. If you want more advanced solar panels & batteries you'll have to learn advanced smelting and build the regular solar panels/station batteries. There are other guides in Stationpedia Ascended's database to instruct you in that direction. For reference below:69

**Building a Solar Panel:**
1. Place the Kit (Solar Panel) on a large grid outside
2. Add a **Glass Sheet** to complete it
3. Connect cables from the panel to your power grid


**Power Output:**
- **Moon:** Up to 750W in direct sunlight
- **Mars:** Up to 455W in direct sunlight
- **Output depends on angle** - panels facing the sun directly produce more

### Station Batteries

Batteries store power for when your solar panels aren't producing.

**Building a Station Battery:**
The Kit (Station Battery) requires crafting - it's NOT in your starter kit. You'll need:
- Electronics Printer or Fabricator (requires Autolathe first)
- 20g Gold, 20g Copper, 20g Steel - see the Smelting Mechanics Page69

**Once you have the kit:**
1. Place it on a large grid
2. Use **Welder + 2 Steel Sheets** to build stage 1
3. Use **Screwdriver** to complete it
 
**Capacity:** 3.6 MJ (1 kWh) - enough to run basic systems overnight

**How it works:**
- Connects to your cable network via input and output ports
- Charges when excess power is available
- Discharges when power is needed


### The Solid Fuel Generator (Starter Kit Option)

Your starter kit includes a **Kit (Solid Generator)**. This burns fuel to produce power - useful for night or emergencies. Try to either build a Station Battery before you build this so you can store the power you make.

**Building:**
1. Place the kit on a frame
2. Connect to your power network with cables

**Fuel Types:**
- **Coal** - 200 kJ per unit, burns 5 seconds
- **Charcoal** - 120 kJ per unit, burns 3 seconds minimum
- **Solid Fuel** - 400 kJ per unit, burns 10 seconds

**Power Output:** Up to **20kW** (but requires heavy cables for full output; regular cables limit you to 5kW)

**Getting Fuel:**
- Coal: Mine it from ore deposits (use Mining Drill or Pickaxe)
- You'll want an **Ore Scanner Cartridge** in your tablet which you have to build. 

### Battery Charger

Your starter kit includes a **Battery Charger**. This charges the small battery cells used in your suit, tablet, and portable devices.

**Setup:**
1. Place on a wall
2. Connect to power network
3. Insert battery cells (holds up to 5)

**Power Draw:**
- 10W standby
- 500W per battery being charged
- Maximum 2,500W if charging 5 batteries

**Charging Times:**
- Small Battery Cell: ~36 seconds
- Large Battery Cell: ~4 minutes 48 seconds

---

## Temperature Control

Your body needs to stay within a safe temperature range.

### Safe Temperature Range

- **Safe:** 0°C to 50°C (273K to 323K)
- **Yellow Warning:** Below 0°C or approaching limits
- **Red Warning / Damage:** Below -10°C or above 50°C

**On the Moon:** External temperature doesn't really exist although the sun shining through windows will heat your base
**On Mars:** Temp can swing between -13c and 5c swings, plus storms can affect temperature

### Portable Air Conditioner (Starter Kit)

Your starter kit includes a **Kit (Portable Air Conditioner)**. This is your first temperature control option.

**How it works:**
- **Cold Mode:** Removes heat from the room, stores it internally
- **Hot Mode:** Releases stored heat into the room

**Power:** 200W (uses battery cells)
- Small Battery: ~1.5 minutes
- Large Battery: ~12 minutes

**Limitations:**
- Cold mode stops working if room exceeds +30°C
- Hot mode stops working if room drops below -10°C
- Internal tank fills up - when it flashes an error, you need to dump the stored heat/cold

**Using it:**
1. Build the kit (place it)
2. Insert a battery cell
3. Set to Cold or Hot mode as needed
4. When tank fills, either:
   - Vent to outside (if you can tolerate the temperature there)
   - Connect to a Tank Connector with wrench to dump into pipes

**For beginners:** This is temporary. It will keep a small room habitable while you set up better systems. BE CAREFUL. THE COOLANT TANK IS FILLED WITH N20 AND WILL EXPLODE IF IT GETS TOO HOT KILLING YOU AND DESTROYING YOUR BASE

### Heating Up

At the beginning, you can build a wall heater described below, or just pop some windows into you base using the Wall Kit. Windows let the sun shine in and will heat your base atmos, so all you will need is to cool the base, never heat it. Printers and another machines also generate heat when operating/turned on.

#Wall Heater

**Requires crafting** with Hydraulic Pipe Bender:
- 3g Iron, 1g Gold, 3g Copper

**Power:** 1010W (significant draw!)

**How it works:** Converts electricity directly into heat. Place on wall, connect power, turn on.

**Good for:** Moon bases where cold is the main problem.

### Wall Cooler

If your base is too hot, you may want a Wall Cooler. Most bases generate a fair amount of heat, so this is a critical step for early game. The Wall Cooler is not power efficient, however, and it is much better to simply use a digital valve with some radiators and a pipe to the outside atmosphere. On the moon & mars outside is cold enough to get your base down to 20c easily. See the Temperature & Base Guide for these techniques 69.

**Requires crafting** with Hydraulic Pipe Bender:
- 3g Iron, 1g Gold, 3g Copper

**Power:** 10W idle, 1000W when cooling

**How it works:** Removes heat from the room and dumps it into a coolant pipe. **You need a pipe network with gas to carry the heat away** - this makes it more complex than the heater.

**For beginners:** Use the Portable AC first. Wall Cooler setup requires using pipes etc. Once you have the portable air conditioner up you can set up wall heaters/coolers. 

---

## Atmosphere Setup

You need breathable air at the right pressure and temperature.

### What You Need to Breathe

**Oxygen Partial Pressure:**
- **Minimum safe:** ~16 kPa oxygen partial pressure
- **Recommended:** ~20 kPa or higher (no warnings)
- **Warning thresholds:**
  - 16-12 kPa: Yellow "Oxygen Low" warning (no damage)
  - 12-5 kPa: Red "Oxygen Critical" warning (no damage yet)
  - Below 5 kPa: You're suffocating

**What is Partial Pressure?**
It's the portion of total pressure from one specific gas.

**Example:** If your room has 100 kPa total pressure and is 20% oxygen:
- Oxygen partial pressure = 100 × 0.20 = 20 kPa ✓ (breathable)

**Example:** If your room has 50 kPa total and is 20% oxygen:
- Oxygen partial pressure = 50 × 0.20 = 10 kPa ✗ (critical warning)

### Filling your Suit Oxygen Tank

Your lander has a **Portable Tank with 7576 kPa of pure oxygen**. You can use this directly. It's best though to use this tank to fill your suit oxygen tank until you can get a system up to sustainably to filter oxygen from other gases and put it in your suit tank. You can also use dirty air (oxygen, nitrogen etc.) in your suit tank if you apply the correct filters. An easy way to get more air in your tank is to use a canister filler with an active vent and suck in base air carefully until you fill the tank. For now, stick with using your Portable Oxygen Tank's canister slot to fill your oxygen tank.


### The Nitrogen/Oxygen Mix (Long-term)

You can save Oxygen by filling most of your base with Nitrogen if available.

**Recommended ratio:** Approximately **3:1 Nitrogen to Oxygen** (75% N2, 25% O2)

**At 100 kPa total pressure:**
- 75 kPa Nitrogen
- 25 kPa Oxygen (well above the 20 kPa minimum)

**Getting Nitrogen:**
- **Moon:** Mine Nitrogen Ice (Nitrice), melt in Arc Furnace
- **Mars:** The atmosphere is mostly CO2, but contains some nitrogen - requires filtering

**For beginners:** Start with pure oxygen (Oxite), add nitrogen later when you have the infrastructure.

### CO2, Pollutant, Volatiles Buildup

When humans breathe, they exhale CO2. Over time, CO2 builds up in your base. This is normally ok because you can use it to feed your plants and it has to be very high to cause poisoning. This is untry for Pollutant and Volatiles, so you want to filter those if any are present. Check your room air makeup with your atmos catridge in your tablet.

**Solutions:**

**Option 1: Portable Scrubber (Starter Kit)**

Your kit includes a **Kit (Portable Scrubber)**.

How it works:
1. Build the scrubber
2. Insert the **Pollutant Filter & Volatiles Filter** in the back. They came with your scrubber.
3. Power with battery (uses 20.5W)
4. It absorbs Volatiles & Pollutant from the room air

When the internal tank fills (8106 kPa), the scrubber flashes an error. You need to empty it:
- Bring it outside
- Pull the lever to vent (releases any gases it is filtering)

**Option 2: Permanent Filtration**

Later, you'll want permanent filtration in your pipe network. This requires:
- Filtration Unit or Air Filtration System
- Gas Filters
- Pipe infrastructure

For now, the Portable Scrubber handles it. If your base is big enough there won't be an issue for a long time.

### Checking Your Atmosphere

Use your **Tablet with Atmospheric Analyzer Cartridge**:
- Shows temperature
- Shows pressure
- Shows gas composition (percentages)

Calculate partial pressure: Total Pressure × Gas Percentage = Partial Pressure

---

## Suit Management

While you're setting up your base, you're still relying on your suit. Once you've got an atmosphere propely set up you can open it.

### Refilling Your Suit Oxygen

**Method 1: Room Atmosphere**

If your base has breathable atmosphere:
1. Open your helmet (press **I** - but only if atmosphere is safe!)
2. Your suit will equalize with room atmosphere
3. Close helmet when leaving

**Method 2: Direct from Portable Tank**

1. Take a **Gas Canister** (small canister, not the portable tank)
2. Place it in the striped slot on top of the Portable Tank
3. The canister fills with oxygen
4. Take the canister
5. Open your suit inventory (press **3**)
6. Replace your air canister with the full one

### Emptying Your Waste Tank

Your suit's waste tank collects filtered CO2. When it exceeds ~4052 kPa, your filters stop working. Vent this inside your base so you can feed your plants!

**To vent:**
1. Press **3** to open suit panel
2. Find your waste canister
3. Click on it, select Open
4. CO2 vents to atmosphere


### Filter Lifespan

Your suit's CO2 filters have limited life:
- **Small Filter:** 2 hours
- **Medium Filter:** 10 hours
- **Large Filter:** 40 hours

Filters only degrade when actively filtering CO2. Check your filter status regularly.

---

## Food and Water

You won't starve in the first hour, but you need to plan ahead.

### Starting Supplies

**In your Orange Uniform (on your body):**
- Cereal Bar
- Water Bottle

**In Survival Supplies crate:**
- 1 Water Bottle Box (4 bottles)
- 2 Cereal Bar Boxes (6 bars each)

**In Your Lander**
- 1 Water Bottle Bag (4 Bottles)
- 1 Cereal Bar Bag (6 Bars)

### Eating and Drinking

**On easier difficulties:** You can consume through your suit.

**On Normal+ difficulties:** You must open your helmet to eat/drink - meaning you need breathable atmosphere first.

**To consume:**
1. Hold the food/water in your hand
2. Open your helmet (if required by difficulty)
3. Left-click to consume

### Food Quality

Food quality affects your maximum hydration:
- Low quality: 75% max hydration
- Best quality: 175% max hydration

Your starting soups are decent quality. For long-term survival, you'll need to grow crops.

### Hunger and Thirst Mechanics

You can let health drop from hunger/thirst and recover later - you don't need to eat constantly. If your health bar gets down to 0 you die.

**Rough timeline (Stationeer difficulty):**
- Starting oxygen: More than a week
- Starting water: About a week
- Starting food: About a week

### Long-term: Growing Food

Your Organic Supplies crate contains seeds:
- Potato, Wheat, Soybean (3 each)
- 6 Fertilized Eggs 

The eggs are going to spoil. Even if you grew them the odds of you having enough wheat to feed the chickens on week 1 are slim to none. Leave the eggs closed and you can rush to build  a fridge to make them last longer. For now...forget about it.

You also have a **Kit (Portable Hydroponics)** in your starter supplies.

**Plants need:**
- Light (sunlight or grow lights)
- Atmosphere (~20°C temperature, appropriate gases)
- Water
- Time

Farming is beyond this beginner guide, but know that it's your long-term food solution.

---

## Your First Night Checklist

Before the sun sets (or before you run out of suit supplies):

**Minimum Viable Base:**
- [ ] Sealed room (walls + door)
- [ ] Power source connected (solar panel + cables)
- [ ] Some atmosphere inside (oxygen from portable tank)
- [ ] Temperature above 0°C (use Portable AC if needed)

**Better Setup:**
- [ ] Automated airlock working
- [ ] Solid Fuel Generator as backup power
- [ ] Battery Charger keeping your suit batteries full
- [ ] Portable Scrubber running for CO2
- [ ] Enough pressure (30+ kPa pure O2, or 100 kPa mixed)

**Before Opening Your Helmet:**
- [ ] Check temperature: Between 0°C and 50°C
- [ ] Check oxygen: At least 20 kPa partial pressure
- [ ] Check pressure: Above 30 kPa total

---

## Summary

**Power:**
- Solar panels for day power
- Solid Fuel Generator for backup
- Batteries for night (requires crafting)
- Don't exceed 5kW on regular cables

**Temperature:**
- Stay between 0°C and 50°C
- Use Portable AC for now
- Wall Heater for cold bases (later)

**Atmosphere:**
- Need ~20 kPa oxygen partial pressure minimum
- 30 kPa pure O2 works for early game
- Watch for CO2 buildup - use Portable Scrubber
- Nitrogen mix (75/25) is safer long-term

**Suit:**
- Refill oxygen before it runs out
- Vent waste tank when it fills
- Replace filters before they expire

**Food/Water:**
- You have about a week of supplies
- Start planning farming soon

---

*You should now be able to survive your first few days. From here, you'll expand: better power systems, sustainable atmosphere, food production, and eventually more advanced technology.*

*Good luck, Stationeer.*

---
