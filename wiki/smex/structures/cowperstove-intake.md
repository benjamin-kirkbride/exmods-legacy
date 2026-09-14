---
title: Cowper stove
version: 0.9.8
---

The [[Cowper Stove Intake]] starts this build and is the front of the finished stove, where the
furnace's exhaust arrives. Stoves are built in pairs; the loop they run and the heat they buy
are on [[Hot blast]].

## The shape of it

A stove is a three by three brick tower, seven levels tall counting the floor, with four Heat Sinks
stacked in the middle column and four gas fittings in two pairs. The Cowper Stove Intake is the
control block and the front of the building.

| where | what |
|---|---|
| the cell below the intake | left open, for the coal pile that speeds charging |
| the intake block itself | front, at floor level: furnace exhaust in |
| one level above the intake | front: the outlet that gives hot blast out |
| the back wall at the intake's level | the outlet that sends spent exhaust on |
| the back wall one level up | the passthrough that takes air in from the blowers |
| the middle column, the intake's level and the three above it | the four heat sinks |
| the side wall at the intake's level | the Iron hatch door |

The table is the whole of the plumbing: the furnace's gas goes to the front pair, the blowers' air
to the back pair, and each pair has its inlet and its outlet at different levels. Swap the levels
of a pair and the stove will charge and never blow.

## Build order

1. Pick a spot beside the furnace with room for two stoves and the pipe between them, and with the
   smoke stack reachable from both.
2. Place the intake facing the way the exhaust main will come in. It is the control block, so its
   facing fixes the stove.
3. Hold ctrl and shift and right-click it for the build outline and the chat list of what is still
   missing.
4. Lay the floor and leave the cell directly under the intake empty.
5. Stack the four heat sinks in the middle column, starting at the intake's own level. A stove
   built without them has to be taken apart again; they are the whole of the machine.
6. Set the Iron hatch door in the side wall and the three pipe fittings in their cells: the
   hot-blast outlet above the intake, the air passthrough high at the back, the spent-exhaust
   outlet low at the back.
7. Build the walls up and cap it. Any refractory tier is accepted and they can be mixed.

## The first charge

1. Pipe the furnace's exhaust main to the intake face, with a valve in the line.
2. Pipe the hot-blast outlet above it to the tuyeres, with a valve.
3. Pipe the blowers to the air passthrough at the back, with a valve, and the spent-exhaust outlet
   at the back onward to the Smoke Stack Intake or the next stove.
4. Run the furnace on cold blast until it is melting. A stove has nothing to charge from until the
   furnace has been alight for a while.
5. Open the exhaust valve only. The panel reads "Heating Up!" and the heat sinks climb; look at one
   to read the temperature. With no coal below, the core closes half the gap to the exhaust in
   about ten minutes; with coal it is under three.
6. Shut the exhaust valve and open the air valve. The panel reads "Heating Air" and the furnace
   picks up the boost.

Build the second stove before you rely on either: a stove that is blowing is a stove running down,
and the pair alternate. The loop, the rates and the ceiling are on [[Hot blast]].

## What goes wrong

**"Cannot heat up, exhaust mixes with the air!"** Both of that stove's valves are open. One at a
time, exhaust first.

**Built without heat sinks.** The multiblock will not complete, and the outline will say so; if the
build was finished some other way the stove has nothing to store heat in.

**The two back fittings swapped.** The passthrough is the air inlet and goes high; the outlet is
the spent-exhaust exit and goes low.

**No overflow line to the stack.** Shutting a stove's exhaust valve leaves the furnace's flue gas
with nowhere to go, and a choked furnace stalls. Build the overflow before the first swap, not
after the first failure.

**Split output networks.** If each stove has its own pipe to its own tuyere, the blast temperature
collapses on every swap. Merge the hot side so both tuyeres draw from the same pipes.

**A stove that will not reheat.** Shutting the air valve leaves gas standing in the passthrough;
the stove vents that itself on the next tick and charges from the one after. A stove that stays on
"Cannot heat up" has an air valve still open.
