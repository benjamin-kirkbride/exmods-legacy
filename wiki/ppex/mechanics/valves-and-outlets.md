---
title: Valves and outlets
covers:
  - "ppex:pipe-valve-*"
  - "ppex:pipe-pressurevalve-*"
  - "ppex:pipe-outlet-*"
  - "ppex:steamcondenser-*"
version: 0.6.8
---

Four fittings shape a run: the hand valve that cuts it, the pressure valve that caps it, the outlet
that ends it at a machine or a chimney, and the condenser that turns its steam back into water.

## The hand valve

Piping (Valve) is a shut-off with no numbers on it. Right-click it with an empty hand to toggle it;
the model's pose shows whether it is open or shut, and unlike the pressure valve it has no
direction and no facing mark. Open, it is an ordinary pipe node and the run flows through it.
Closed, it severs the network at its own cell, and the two sides become separate networks with
separate pressures.

Two things surprise players.

Opening a valve merges two networks into one pool, and the merged run settles at one pressure at
once. A charged run opened into an empty one reads the average, which looks like the pressure has
been dumped. It has not gone anywhere, it is spread over more pipe.

A closed valve does not cap the far side. If nothing is built past it, that face is an open end for
the run on the other side and bleeds 8 L/s like any other hole. A run that is still audibly leaking
after you closed a valve needs a pipe or a port built past the valve. Some players have also found a
freshly placed valve passing flow until they toggled it once; if a run reads as one network across a
closed valve, toggle it and check again with the highlight.

## The pressure valve

Piping (Pressure Valve) is a directional overflow. It watches the network on its input face and
spills everything above its gate pressure into the network on its output face.

| fact | value |
|---|---|
| gate on a freshly placed valve | 1 atm |
| step per interaction | 0.25 atm |
| lowest gate | 0 atm |
| highest gate | the valve's own rating, 5 atm iron and 10 atm steel |

Right-click it with an empty hand to raise the gate, sneak and right-click to lower it. The
copper-trimmed ring marks the input side, and a wrench flips the direction.

It only flows downhill. Gas or water crosses only while the output side sits below the input
pressure, so a pressure valve can never push a loop round past itself and is **not** a backflow
preventer. An overflow wired into a line that already sits at or above its source moves nothing.

With nothing piped to its output face, the valve vents the overflow to the air at the ordinary leak
rate, 8 L/s of gas or 10 L/s of water, with a plume to show it. That is the usual way to use one.

Where it goes: **not** in the line between a strong boiler and a weaker engine. A pressure valve
is a network endpoint, so one set in-line splits the run in two and then feeds the engine's side
until both sides match, which is the pressure it was supposed to prevent. Hang it off a junction on
the run the engine is on, so that run is the valve's input side, and let the output side vent to
air or to an overflow line. A Cornish Boiler at 5 atm in front of a Watt Engine that wears above
4 atm is the case that needs one, gated at 2.5 to 3 atm.

On water it does not throttle, it dumps: once the feed pressure tops the gate it moves the whole
line into the output side, or sprays it out of an open face.

The tutorial videos in circulation predate pressure entirely and the valve's interface has changed
since, so set the gate from the look-at line rather than from a video.

Players running Steelmaking Expanded use the same fitting on the furnace exhaust, gated well under
the furnace's 0.8 atm choke; that setup is on the
[Steelmaking Expanded wiki](/current/smex/Home/).

## Outlets and passthroughs

Pipe Outlet is a one-faced pipe that ends a run against a machine's port face. Pipe Passthrough
(Straight) and Pipe Passthrough (Bend) are the same pipe cast into masonry, so a wall can cross a
run: the boiler fireboxes and the furnace walls of Steelmaking Expanded are built from them. All
three come in fireclay and the seven coloured brick finishes, and none of them ever bursts or caps
a run's pressure.

Stand an ordinary chimney on the open top connector of any of the three and the network vents
16 L/s of gas through it, smoking, instead of leaking 8 L/s. That is how a firebox is exhausted.

An outlet that faces the wrong way can be turned with a wrench, which cycles the facings that are
valid in that cell. Where only one facing is valid there is nothing to cycle and the wrench does
nothing: break it, build the pipe that decides its shape, and place it again.

## The steam condenser

The Steam Condenser is a T-shaped fitting. The stem is its steam port; the bar is a water line
running straight through it. It draws up to 30 L/s of steam from the stem, condenses it at the
usual 16 L of steam to 1 L of water, and adds that to the water crossing the bar, which it passes
through at up to 50 L/s. The fuller of the two bar faces is the inlet and the other is the outlet,
and the inlet's pressure carries on downstream.

It is a connector, not a node: the three runs that meet it stay three separate networks and the
condenser bridges the two water sides itself.

With no water line at all, the steam it draws vents as gas. With water coming in and no outlet
line, the backed-up water sprays out. Both are visible, and both mean it is throwing away what it
was built to reclaim.

## What goes wrong

**The run will not hold pressure and every valve is shut.** Look for the face past a closed valve,
or past a pressure valve's output, with nothing built on it.

**The pressure valve does nothing.** It is the wrong way round (the copper trim is the input), its
gate is above the pressure the run ever reaches, or its output side is at or above its input side.

**An engine breaks behind a boiler that has a relief valve.** The relief valve has to sit on the
engine's own run, holding that run down. One on the boiler's side of a closed valve protects
nothing.
