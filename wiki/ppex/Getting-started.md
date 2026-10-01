# Getting started with steam power

Everything in this mod is one chain: water feeds a boiler, the boiler drives an engine, and the
engine turns exactly one sub-machine. A plant works when that chain closes into a loop that keeps
its own boiler full while the coal burns. This page builds the smallest loop that does, in the
order to build it.

It is iron-age work. Before you start you want a wrench, a pond or a lake, and enough iron for a
boiler, an engine and a few dozen pipes.

## What to have ready

| what | why |
|---|---|
| [[Fluid Intake]] | the only thing that makes water |
| [[Manual Fluid Pump]] | fills the boiler before there is any steam |
| [[Cornish Boiler (block)]] frame, then 22 iron or steel plates, 16 nails and strips, 8 rods and 44 fireclay bricks | the vessel, raised in three right-click stages |
| [[Pipe Passthrough (Bend)]] and [[Pipe Passthrough (Straight)]] in fireclay, and a fireclay [[Pipe Outlet]] | the firebox walls carry the water line and the exhaust |
| fireclay bricks and an iron hatch door | the firebox itself |
| a chimney | to vent the firebox |
| [[Watt Engine]] frame, then 4 plates, 24 rods, 12 nails and strips and 36 fireclay bricks | the engine, raised the same way |
| [[Piping (Pressure Valve)]] | keeps the boiler from breaking the engine |
| [[Fluid Pump]] | the sub-machine that closes the water loop |
| [[Piping (Straight)]], bends and junctions | two or three dozen covers a first plant |

Iron pipe is fine for everything here. Steel only becomes necessary at the Lancashire Boiler.

## 1. Water

Set a Fluid Intake on open water with a full three-deep cube of water directly under it, and keep
it at least 6 blocks from any other intake. Pipe its output to where the boiler will stand.
Nothing in this mod creates water: every pump only moves what an intake produces.

## 2. The boiler

Build a Cornish Boiler. Its own page has the footprint, the build order and the first firing; the
parts that matter to the rest of this page are that feedwater enters underneath through a fireclay
Pipe Passthrough (Bend), steam leaves the top, and exhaust leaves the far end.

Run the intake line into a Manual Fluid Pump and the pump's delivery into the passthrough under
the boiler. Hold right-click on the pump to crank it. It moves 2 L/s, which is slow, and it is
enough to reach the 150 L the boiler needs.

## 3. The exhaust

Stand a chimney on the fireclay Pipe Outlet, which sits at the far end of the boiler, opposite the
firebox. A boiler makes 16 L/s of exhaust and a chimney draws exactly that. The fire draws only
through a chimney or a smoke stack: an open pipe end carries the exhaust away but gives no draught.
Get this wrong and the fire snuffs itself ten seconds after you light it.

## 4. Steam, and the valve that saves the engine

Pipe the boiler's top connector toward where the engine will stand. Put a Piping (T-Junction) in
that run and hang a Piping (Pressure Valve) off the branch, with the valve's copper-trimmed side
facing the run and its other side open to the air or piped to a vent. Set the gate to about 2.5
atm: right-click the valve with an empty hand to raise it in steps of 0.25 atm, sneak and
right-click to lower it.

This is not optional. A Cornish boiler chokes at 5 atm and a Watt Engine starts wearing toward a
burst above 4 atm, so an ungated line breaks the engine in a minute of running. Hang the valve off
the run the engine is on, not in the line between the two: a pressure valve holds down the side
its input face reads, and one set in-line feeds the far side until both sides match instead.

## 5. The engine

Raise a Watt Engine beside the line and bring the steam pipe to its inlet face, which is at its
back: the machine always faces one way and there is no rotate. It engages at 2 atm and delivers
its power to whatever sits in its drive cell, and to nothing else. An engine with no sub-machine
draws no steam at all, which is not the same as safe: the over-pressure timer still runs, and a
relief valve cannot hold down a run nothing is drawing from. Shut the steam line or let the fire
die before you leave an engine standing empty.

Its condensate drain is on the side. Pipe it back toward the boiler if you want that litre a
second back, or leave it to spill.

## 6. The sub-machine, and closing the loop

Set a Fluid Pump in the engine's drive cell. It snaps itself to the right facing. Pipe the intake
line into the pump's underside and its delivery up into the boiler's feedwater passthrough, and
the loop is closed: the boiler makes steam, the steam drives the engine, the engine works the
pump, the pump keeps the boiler full. Behind a Watt engine the pump moves 30 L/s, which is far
more than the 2 L/s the boiler is boiling away.

Now you can stop cranking by hand.

## 7. What to add next

- A Mechanical Power Generator instead of the pump, to turn axles. It needs its own engine and
  boiler, because one engine drives one sub-machine.
- A Steam Condenser on the return, to get spent steam back as feedwater.
- A Lancashire Boiler and a Cornish Engine for the high-pressure tier, on steel pipe.
- An air blower from Steelmaking Expanded, which is the reason most people build steam at all: the
  blast furnace and the Bessemer converter take blast air and no waterwheel supplies it.

## Where the details are

Pipes and pressure for capacity, leaks and bursting. Boilers for water, fire and the explosion.
Steam engines for the bands, the throttle and repair. Mechanical power and pumps for shaft load
and the pumps. Valves and outlets for the fittings. Commands and config for the numbers and how to
change them. Compatibility for other mods.
