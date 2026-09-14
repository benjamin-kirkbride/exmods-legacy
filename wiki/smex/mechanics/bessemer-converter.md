---
title: Bessemer process
covers:
  - "smex:convertercontrol*"
  - "smex:converterbessemer*"
  - "smex:converter-intake*"
  - "smex:convertertransmission*"
order: 6
version: 0.9.8
---

The Bessemer converter blows air through a bath of molten iron, burns the carbon out of it and
leaves mild steel. It is the last stage of the line and the most demanding machine in the mod: it
needs molten iron delivered by canal, mechanical power to tilt the vessel, and blast at 2.5 atm,
which only a steam-driven Air Blower reaches.

## What it needs running

| service | requirement |
|---|---|
| blast | Air medium at 2.5 atm or more at the Bessemer Gas Intake |
| mechanical power | an axle on the Bessemer Transmission, turning above 0.1 geared speed; the vessel loads the shaft by 0.25 |
| metal in | a canal delivering molten iron to the input tap above the vessel |
| metal out | a canal start under the vessel to take the finished steel |

The gas intake and the transmission must face the same way as the control block. Any other
orientation completes the multiblock and then refuses to run, with "Gas intake misaligned!" or
"Transmission misaligned!" on the panel.

The intake is a fixed port, not a pipe: a pipe has to sit in the cell in front of it, presenting a
connector back at it. A blower bolted straight onto the intake feeds nothing, which is the single
most common reason a converter sits at 0.00 atm with the boilers roaring.

## Pressure is the throttle

Everything the blow does scales off the pressure above the 2.5 atm gate: how fast it runs, how much
air it eats, and how hot the bath sits. A charge takes 300 seconds of blast at a conversion rate of
1.0.

| blast at the intake | rate | a full blow takes | air drawn | bath | cold scrap it can carry |
|---|---|---|---|---|---|
| 2.5 atm | 0.50x | 10 min | 12 L/s | 1800 C | 375 units, 75 bits |
| 3.0 atm | 0.71x | 7 min | 17 L/s | 2042 C | 677 units, 135 bits |
| 4.0 atm | 1.14x | 4 min 22 s | 27 L/s | 2196 C | 869 units, 173 bits |
| 6.0 atm | 2.00x | 2 min 30 s | 48 L/s | 2283 C | 979 units, 195 bits |

6 atm is where the gains stop, and it is also as hard as a Cornish engine driving an Air Blower can
blow: its 8 atm break pressure through the engine's 0.75 efficiency. Air short of the demand slows
the blow in proportion, exactly as it does at the furnace.

## The charge

The vessel holds 2400 units, counting molten metal and cold scrap together.

Molten iron arrives through the input tap while the converter is set to Filling. It takes one metal
at a time; a second metal is refused with "Metal type mismatch!".

Cold scrap is charged by hand: hold iron or steel bits and right-click the vessel's upper hatch,
not the control block. The click takes everything in the active hand, limited only by the room
left in the vessel, and nothing from the rest of the hotbar; to put in less, split the stack
first. A bit is worth 5 units. Scrap goes in before the blow, alongside a raw iron charge or into
an empty vessel; it will not go into finished steel.

Scrap is cold mass on the heat balance: every unit of it costs the bath 0.8 C, and the bath has to
stay at 1500 C or the blow stalls. That is the whole of the limit, which is why the table above
gives the allowance per pressure rather than a fixed cap. The panel prints what the scrap in the
vessel is costing. Until the blow ends the scrap is heat load and not metal; at the end it joins
the heat unit for unit and pours with it.

## A run

1. Set the converter to Filling: sneak and right-click the control. Open the canal tap feeding the
   input.
2. Charge scrap on the vessel hatch if you are remelting any.
3. Set the converter back to Normal: a plain right-click on the control. With power and blast both
   present it starts refining, and the panel counts "Refining... 34%".
4. Watch for "Steel ready! Pour it out." That is how you know the steel is done; the vessel stops
   drawing air and holds the heat.
5. Set it to Pouring: sprint and right-click the control, held for one second. The steel drains
   into the output canal start and travels the molten network exactly like iron.

## What goes wrong

**"Refining paused! Needs air".** No blast is reaching the intake. Check the pipe in front of the
intake, the medium (air, not exhaust), and the pressure.

**The pressure sags mid-blow.** The converter's draw climbs with the rate, up to 48 L/s at 6 atm,
and the blow does not ease off when the line cannot keep up. Build the supply for the rate you mean
to run, not for the starting pressure. A drop to about 2.48 atm partway through a blow was reported
in September 2026 and has no recorded fix; watch the reading through the run rather than only
before it.

**"Bath too cold to refine, needs 1500 C."** Too much cold scrap for the pressure behind it. Blow
harder or charge less, and check the table above.

**Nothing happens when you click the control.** No mechanical power. The control's own panel prints
"Power: stopped" when the transmission's axle is not turning.

**"To much solidified residue! Break the converter to clear it".** The charge froze. Under 480
units, a fifth of the vessel, it can be chiselled out of the upper hatch once it is hardened;
above that the vessel has to be broken. Breaking returns all of its construction materials, the
metal as bits less a few units mangled in the process, and any unmelted scrap in kind.

**The vessel will not go up.** It needs one large gear and eight iron or steel rods in your hotbar,
and a clear 3x3x3 volume: "The converter cannot be placed! Clear the 3x3x3 space for it first."

**A mechanical blower will not do it.** The Twin-Tub Blower seals at 2.0 atm, under the gate. That
gap is deliberate: mechanical power makes iron, steam makes steel.

## Throughput

A blow takes the same time whatever the vessel holds, so the way to get throughput out of a
converter is to fill it before blowing it. A full 2400-unit vessel blown at 6 atm is 16 units of
steel per second while it runs, and less once filling and pouring are counted, against the
20.4 units of iron per second a blast furnace on hot blast makes. A furnace driven hard therefore
outruns one converter; two converters to one furnace is the owner's own figure, given in June 2026
and never revised.
