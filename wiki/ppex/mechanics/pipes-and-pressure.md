---
title: Pipes and pressure
covers:
  - "ppex:pipe-straight-*"
  - "ppex:pipe-bend-*"
  - "ppex:pipe-tjunction-*"
  - "ppex:pipe-xjunction-*"
  - "ppex:pipe-passthrough-*"
  - "ppex:pipe-passthroughbend-*"
order: 1
version: 0.7.0
---

Every pipe you connect together forms one network with one shared pool. A network carries one
medium at a time, either a gas (air, steam or exhaust) or water, and it holds one temperature and
one pressure over its whole length. Machines are not part of the run: a boiler, an engine or a pump
sits beside it and pushes into or draws out of the pipe that meets its port face.

## The numbers

| fact | value |
|---|---|
| capacity per pipe | 30 L at 1 atm |
| gas pressure | stored volume divided by capacity, read as gauge atm |
| iron pipe burst pressure | 5 atm |
| steel pipe burst pressure | 10 atm |
| grace at burst pressure before a pipe lets go | 30 s |
| gas bled by an open end | 8 L/s per open end at 1 atm, in proportion to the run's pressure |
| water drained by an open end | 10 L/s per open end |
| gas drawn by a chimney on a passthrough or outlet | 16 L/s per chimney |
| evaporation from a water run | 50 L per in-game day |

Pressure is the stored volume divided by the run's capacity, and the capacity is 30 L per pipe. A
ten-pipe run reads 1 atm at 300 L and 2 atm at 600 L; an empty run reads 0. The look-at line marks
it `(g)`, a gauge reading, so steam temperature is taken one atmosphere higher: 100 C times the
fourth root of the reading plus one, which is 119 C at 1 atm, 132 C at 2 atm, 157 C at 5 atm and
190 C at 12 atm.

Water behaves differently. It cannot be packed past the run's capacity, so a water line reads its
fill ratio while it is filling and jumps to the delivery head of the pump feeding it once it is
brim full, 1 atm from the Manual Fluid Pump, 1.5 atm from the Mechanical Fluid Pump, and the
engine's inlet steam pressure times 0.75 from the Fluid Pump. A pressure valve the line tops holds
it down to the valve's gate for every machine drawing from it, so a boiler fed from a main with a
relief valve gated at 0.5 atm takes its water at 0.5 atm.

A gas run cools 2 C per second toward 20 C, whether or not anything is drawing from it, and a
leaking one cools 5 C per second on top of that. Pressure does not change with it.

## Bursting, and the passthrough limit

Only the four plain shapes take part in over-pressure failure: Piping (Straight), Piping (Bend),
Piping (T-Junction) and Piping (X-Junction). The weakest of them sets the ceiling for the whole
run, so one iron segment caps an otherwise steel line at 5 atm. Hold that ceiling for 30 s with
nowhere to vent and one pipe at that rating, chosen at random, lets go: it drops its materials, its
cell becomes air, and the run splits in two at the gap. Any relief that drops the pressure resets
the 30 s.

Everything else on a run is exempt by design. Pipe Passthrough (Straight), Pipe Passthrough (Bend),
Pipe Outlet, Piping (Valve), Piping (Pressure Valve), Fluid Intake and the tuyeres and ports of
Steelmaking Expanded neither burst nor cap the pressure. A run built only from those has no rating
at all and will never burst, whatever it reaches; a run with one plain pipe in it is limited by
that pipe, wherever in the line it sits.

That matters most on a steam main. A Lancashire Boiler chokes at 12 atm, above the 10 atm of steel
pipe and far above the 5 atm of iron, so a Lancashire line that is left with nowhere to send its
steam will burst its own pipes. Keep a consumer or a pressure valve on it.

## Open ends

Any connector left facing air is an open end, and every open end leaks. Gas costs the run 8 L/s
per open end at 1 atm, in proportion to the run's pressure, so a run at half an atmosphere loses
4 L/s through each end and two open ends lose twice what one does. Water drains at 10 L/s per open
end. A leaking run is also capped at 1 atm however hard it is fed: a producer is held to that
ceiling while any end is open, so a Watt Engine, which engages at 2 atm, never starts on a line
with a hole in it. A gas run fed slower than its ends leak settles below 1 atm, where the leak
matches the feed, and an unfed one runs down toward empty. A boiler on a steam run with an open end
blows itself down to about 1 atm; Boilers has the numbers.

A chimney is the exception. Stand an ordinary chimney on the open top connector of a passthrough,
a passthrough bend or an outlet and the network vents 16 L/s through it instead of leaking there,
smoking while it does. That is the way to exhaust a firebox: 16 L/s is exactly what a boiler makes,
and a boiler's fire draws only through a chimney or a smoke stack. An open pipe end carries
exhaust away but gives the fire no draught.

## Laying a run

1. Choose the material for the pressure you expect, not the pressure you have today. Iron carries
   5 atm, steel 10 atm, and mixing them means the iron decides.
2. Place pipes toward the machine. Straights, bends and junctions orient themselves to their
   neighbours as you place them.
3. Turn a piece that guessed wrong with a wrench. The wrench cycles only the orientations that are
   valid in that cell, so a piece with one valid orientation will not turn at all: break it, place
   the neighbour that decides its shape, and place it again.
4. Butt the last pipe straight against the machine's port face, or end the run in a Pipe Outlet
   against it.
5. Cap or connect every other end. A run with an open end never rises above 1 atm.
6. Check the result with `.exmod network hi`. Every network is tinted its own translucent colour,
   the colours hold still as you work, and the highlight refreshes about four times a second, so a
   run that is secretly two networks shows up at once. `.exmod network unhi` turns it off.

## What goes wrong

**The whole run reads 0 atm or 1 atm and the machines will not engage.** Something is open. Walk
the run with the highlight on and look for the connector nobody built against: a valve with nothing
past it, an outlet facing a wall it does not touch, a junction with a spare arm.

**Two runs that should be one are two colours.** A pipe between them has no connector facing the
other, or a closed Piping (Valve) sits in between. A closed valve severs the network at its own
cell by design.

**The run carries the wrong thing.** A network claims its medium from the first producer and holds
it until it empties, and a water run refuses gas outright. Where two gas runs merge, exhaust
outranks air, so joining a firebox line into a blast line turns the lot into exhaust. Split them
with a valve or a pressure valve.

**Pipes burst on login or after a restart.** A pressure spike on chunk load has been reported to
put a whole plant past 20 atm and burst pipes and boilers that had relief valves fitted. There is
no fix as of 2026-09-14. Vent and stop a pressurised run before a restart rather than trusting it
to survive one.
