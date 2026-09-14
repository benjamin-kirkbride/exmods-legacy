---
title: Mechanical power and pumps
covers:
  - "ppex:enginempgenerator-*"
  - "ppex:enginefluidpump-*"
  - "ppex:manualfluidpump-*"
  - "ppex:mpfluidpump-*"
  - "ppex:pipe-fluidintake-*"
order: 4
version: 0.6.8
---

A steam engine's power leaves it through whichever sub-machine sits in its drive cell. Two of them
come with this mod: the Mechanical Power Generator, which turns axles, and the Fluid Pump, which
moves water. Two more pumps need no engine at all.

## The mechanical power generator

The generator is a constant-power source, not a constant-speed one. Its budget is twice the
engine's current power, so 0.6 from a Watt Engine and 0.4, 0.8 or 1.6 from a Cornish Engine on its
three settings, and the shaft settles where budget divided by load lands:

- Load equal to the budget turns at rated speed, 1.0.
- Twice the budget turns at half speed, and the look-at line adds that the shaft is labouring under
  the load.
- Four times the budget is where the generator runs out of torque altogether and the shaft stands
  still.

Nothing is damaged by overloading it; an overloaded line simply crawls. The game's own handbook says
a generator overstresses to a halt past roughly double its load, which is the labouring point, not
the stall.

Speed has a second ceiling: the shaft can never turn faster than the engine turns it. That is why
chaining a second engine onto one line buys load capacity and not speed, and why a throttled or
steam-starved engine drives its line slowly. Gearing still works normally on a branch.

**The clicking.** The generator reuses the vanilla planetary-gear sound as its working sound, which
is a known mistake; the block is due to be replaced by a crankshaft. The Cornish engine's separate
anvil-like clank is deliberate, the piston reaching the bottom of the cylinder. This mod's sounds do
not yet follow the game's volume sliders. If machine sound breaks all game audio, that is the
looping-sound leak that has been found twice from different sources: report the name of the sound
that is looping, not a video, because the name is what locates it.

On 1.22 this mod also holds the vanilla water wheel's gearing ratio steady across its water check.
Without that, a wheel sharing a network with these machines resets its ratio on every world load and
the whole line runs at the wrong speed.

## Moving water

Every pump here is a transfer device. None of them makes water: a Fluid Intake on the source line
is what produces it, and a pump with no working intake on its source runs and moves nothing, which
its look-at line says.

| pump | rate | delivery head | driven by |
|---|---|---|---|
| Manual Fluid Pump | 2 L/s | 1 atm | holding right-click |
| Mechanical Fluid Pump | 20 L/s per unit of axle speed | 1.5 atm | an axle |
| Fluid Pump | 100 L/s per unit of engine power | engine inlet pressure times 0.75 | a steam engine |

On an engine that means 30 L/s behind a Watt Engine and 20, 40 or 80 L/s behind a Cornish Engine.
The game's own handbook gives about 5 L/s, from before the 0.6.7 rebalance and wrong by a factor of
six or more. The Mechanical Fluid Pump's throughput is a straight proportion of axle speed with no
threshold and no ceiling of its own, so a barely turning shaft moves a trickle; its handbook figure
of 8 L/s, a floor at speed 0.5 and a full rate at 1.5 all predate the same rebalance. Its head
never changes with speed, because the beam lifts the same column however fast it runs, and it loads
the shaft with 0.05 of its own friction plus 0.05 per atm of head.

The faces matter. The engine Fluid Pump draws from the line under it and delivers out of its left
side. The Manual Fluid Pump draws through the face its crank support stands on and delivers out of
the opposite one. The Mechanical Fluid Pump couples its axle on the side of its near, low cell and
draws in under the far, low one; its delivery leaves the high cell's inward face, back over the
crook of the L, so the delivery main runs into the empty cell above the pump's middle one, not off
the far end.

## The fluid intake

A Fluid Intake only draws while the whole 3 by 3 by 3 cube of water directly below it is water. A
frozen skin on the outer cells of the top layer is tolerated, but the cell immediately below must
stay liquid, so a pond that freezes right under the intake stops. A second intake within 6 blocks
disables it, which is deliberate: intakes cannot be packed onto one pond.

## Closing the water loop

1. Set a Fluid Intake on open water at least three blocks deep and pipe its output away.
2. Put a pump on that line. Before there is any steam that is the Manual Fluid Pump, or the
   Mechanical Fluid Pump if axles already turn.
3. Run the pump's delivery line into the passthrough bend under the boiler. The boiler draws from
   it at up to 10 L/s and stops at half its capacity.
4. Once the boiler drives an engine, give that engine a Fluid Pump and let it take over the feed.
   A plant that also wants a blower or a generator needs a second engine.
5. Pipe the engine's condensate outlet back toward the boiler, and add a Steam Condenser on the
   return if you want the spent steam back as well.

## What goes wrong

**The pump says it has no intake.** The intake is not on the same network as the pump's source
face, its water cube is not full, or another intake sits within 6 blocks. Turn the network
highlight on and check that the intake and the pump's source face really share one colour.

**The shaft crawls.** The load is past the engine's budget. Throttle a Cornish engine up, add an
engine to the line, or take machines off it. Speed will not rise past what the engine itself turns.

**The delivery line will not push into the boiler.** A water run cannot be packed past its
capacity: it reads its fill ratio while filling and only carries the pump's head once it is brim
full. A long delivery line takes a while to come up to pressure.
