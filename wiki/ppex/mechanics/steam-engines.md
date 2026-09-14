---
title: Steam engines
covers:
  - "ppex:enginewatt-*"
  - "ppex:enginecornish-*"
version: 0.6.8
---

An engine takes steam from the pipe on its inlet face and drives exactly one sub-machine seated in
its drive cell. It makes no power of its own account: the sub-machine decides what the power does,
and an engine with no sub-machine attached draws no steam at all.

## The two engines

| fact | Watt | Cornish low | Cornish normal | Cornish high |
|---|---|---|---|---|
| engages at | 2 atm | 5 atm | 6 atm | 7 atm |
| wears above | 4 atm | 8 atm | 8 atm | 8 atm |
| steam drawn | 30 L/s | 8 L/s | 16 L/s | 32 L/s |
| power delivered | 0.3 | 0.2 | 0.4 | 0.8 |
| shaft load it holds | 0.6 | 0.4 | 0.8 | 1.6 |
| condensate out | 1 L/s | 0.3 L/s | 0.6 L/s | 1.2 L/s |

The Watt Engine runs on a lot of low-pressure steam and is built from iron or steel. The Cornish
Engine is steel and meters its steam with control rods, so it wrings more work out of less: on
normal it does more than a Watt on half the steam.

The engine draws its full steam rate whenever it is engaged, and the power it delivers scales with
the share of that steam the line can actually supply. A starved line does not stop an engine, it
makes it weak.

An engine also sets the output pressure of its sub-machine, at 75 per cent of its own inlet steam
pressure. That is what a pump lifts against and what a blower blows at.

## Matching an engine to a boiler

A Cornish Boiler chokes at 5 atm. That is above the Watt's 4 atm wear point, so a Watt behind a
Cornish boiler needs a Piping (Pressure Valve) gated around 2.5 to 3 atm, hung off the run the
engine sits on and venting to air or to an overflow, or it will break. It is also only just at the Cornish engine's low setting and below its normal and
high settings, so a Cornish engine wants a Lancashire Boiler, which chokes at 12 atm.

Steam supply is the other half of the match. A Cornish boiler makes 32 L/s, which is one Watt with
nothing to spare, or two Cornish engines on normal. A Lancashire makes 48 L/s, which is three
Cornish engines on normal or one on high with a Watt beside it.

## Throttle, breaking and repair

The Cornish engine's control rods answer a wrench on the engine's own cell and on the cell directly
above it: right-click raises the setting, ctrl and right-click lowers it. The look-at line names the
setting and the band it runs in.

Above the wear pressure an engine runs hot for 60 s and then bursts. It is inert until repaired,
and dropping back into the band before the 60 s are up clears the count. Right-click a broken
engine with a wrench in hand to read the bill, then right-click again with the materials in your
hotbar. A Watt takes 4 iron or steel plates and 2 iron or steel rods; a Cornish takes 4 steel plates
and 2 steel rods.

Spent steam leaves as hot condensate on the engine's water outlet face, 1 L/s from a Watt. A water
line there takes it, at no pressure of its own; an unplumbed face sprays it on the ground.

## Why they feel weak, and what they are for

There is no kilowatt or horsepower figure anywhere in this mod, and the power numbers above are
bare ratios. What they buy is shaft load: an engine's Mechanical Power Generator holds a load of
twice the engine's power at full speed, so 0.6 for a Watt and 1.6 for a Cornish on high. A vanilla
helve hammer resists about 0.175, which puts a Watt at about three hammers and a Cornish on high at
about nine. A waterwheel out-pulls both. The in-game handbook still says two and six; those figures
are from before the 0.6.6 rebalance that raised the load an engine holds per unit of power.

So a steam engine is not a better waterwheel. What it is instead:

- Power where there is no river, underground or in a walled shop.
- Power on demand, which starts and stops with the fire rather than the season.
- The only drive for the air blowers of Steelmaking Expanded, which is what the blast furnace and
  the Bessemer converter need and what no waterwheel provides.

One engine drives one sub-machine. A plant that needs a blower and a pump needs two engines, and
usually two boilers, because the steam adds up.
