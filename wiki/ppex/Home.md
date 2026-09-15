# Pipes and Power Expanded (ppex)

Pipes and Power Expanded turns boiling water into mechanical power. It adds iron and steel pipe
networks that carry a gas or water under pressure, two coal-fired boilers, two steam engines, the
sub-machines an engine drives, and the pumps and fittings that tie them together. It is the
infrastructure layer of the Expanded family and a hard dependency of Steelmaking Expanded, whose
blast furnace and Bessemer converter run on blast air that only a steam-driven blower supplies.
This is version 0.6.8 and it needs Expanded Library 0.7.2.

## Contents

The mechanics, in the order a plant is built:

1. [[Pipes and pressure]] - one pool per network, and the leaks, pressure and bursting that
   follow from it.
2. [[Boilers]] - the two vessels, their water and their fire, and the thirty seconds that end in
   an explosion.
3. [[Steam engines]] - the pressure bands, the throttle, repair, and what the power figures mean.
4. [[Mechanical power and pumps]] - the sub-machines an engine drives, and the three ways to move
   water.
5. [[Valves and outlets]] - the fittings that cut a run, cap it, end it at a machine or condense
   it.
6. [[Commands and config]] - every number the mod uses and how to change it in game.
7. [[Compatibility]] - what this mod needs and what it patches.

The two structures:

- The [[Cornish Boiler]], the iron-age entry vessel.
- The [[Lancashire Boiler]], the steel tier that carries the pressure a Cornish Engine wants.

The machines:

- [[Watt Engine]], the low-pressure engine that runs on a lot of steam.
- [[Cornish Engine]], the high-pressure engine with a throttle, which wrings more work from less.
- [[Manual Fluid Pump]], a hand crank for filling a boiler before there is any steam.
- [[Mechanical Fluid Pump]], an axle-driven pump for a shaft already turning.
- [[Fluid Pump]], the sub-machine an engine drives to move water.
- [[Mechanical Power Generator]], the sub-machine an engine drives to turn axles.

The fittings:

- [[Piping (Straight)]], [[Piping (Bend)]], [[Piping (T-Junction)]] and [[Piping (X-Junction)]],
  the plain pipe a run is built from.
- [[Piping (Valve)]], a hand-operated shut-off, and [[Piping (Pressure Valve)]], a directional
  overflow.
- [[Pipe Outlet]], which ends a run at a machine, and [[Pipe Passthrough (Straight)]] and
  [[Pipe Passthrough (Bend)]], the same pipe cast into masonry.
- [[Fluid Intake]], the only thing that makes water, and [[Steam Condenser]], which turns spent
  steam back into it.

## Where to start

New here, start at [[Getting started with steam power]], which builds the smallest plant that feeds
itself, in order. The [[FAQ]] answers the questions players actually asked, and points at the page
that holds each one.

This mod's successor is Iron Industry Expanded, a new mod built on Expanded Library 0.8 rather than
an update to this one, and worlds do not carry over between them. There is no guard against a newer
library either: install Expanded Library 0.8 or later and the game simply stops loading this mod.
Expanded Library 0.8 itself logs one error naming the clash and repeats it to every joining player.
