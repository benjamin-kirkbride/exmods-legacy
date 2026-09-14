---
title: Bessemer converter
---

The [[Bessemer Control]] starts this build and is the block you work from afterwards: filling,
normal and pouring are all set on it. The blow itself, and what it needs running, is on
[[Bessemer process]].

## The shape of it

The converter is a line of four things in a row with its service canals down one side. Taking the
Bessemer Control as the front, and counting cells away from it:

| where | what |
|---|---|
| the control block itself | where you set filling, normal and pouring |
| directly under the control | the Bessemer Transmission, which the axle drives |
| two cells out | the vessel, a 3x3x3 block raised in place |
| four cells out | the Bessemer Gas Intake, where the blast arrives |
| beside the vessel, one level up | the input Molten Canal (Tap) and the canal straight feeding it |
| beside the vessel, two levels down | the output Molten Canal (Start) and the straight leading away |

The structure filler cells are not something you place. They are the rest of the volume the vessel
occupies, reserved automatically when it goes up; all they need from you is to be empty first. Clear
a box four wide, five deep and four tall before you start.

## Build order

1. Place the Bessemer Control where you want to stand. Its facing sets the machine's direction and
   nothing else can be turned afterwards to match.
2. Hold ctrl and shift and right-click it for the build outline and the chat list of what is
   missing.
3. Set the transmission directly under the control and the gas intake four cells out, **both facing
   the same way as the control**. Any other facing completes the structure and then refuses to run,
   with "Gas intake misaligned!" or "Transmission misaligned!" on the panel.
4. Lay the two canal runs beside the vessel's cell: the tap and its straight above, the start and
   its straight below.
5. With one large gear and eight iron or steel rods in your hotbar, right-click the control. That
   raises the vessel two cells out and reserves the volume around it. If the space is not clear it
   says "The converter cannot be placed! Clear the 3x3x3 space for it first."
6. Build the vessel up through its right-click stages. Over all the stages it takes 40 iron or
   steel plates, 42 nails and strips, 18 rods, 84 tier-2 refractory bricks, 84 fire clay and 3
   straight gas pipes. Tier 2 refractory specifically: this is the one build in the mod that does
   not take any tier.
7. Connect the services: an axle to the transmission, a pipe in the cell in front of the gas
   intake, and a canal from a furnace tap into the input straight.

The vessel leans to one side. That is deliberate and there is no mirrored variant; starting the
build from the other side flips the whole block, so decide which way it should lean before you
raise it (owner, 2026-07).

## The first heat

The full procedure and the numbers behind it are on [[Bessemer process]]. In short:

1. Confirm the control reads "Power: running". No power and the levers do nothing at all.
2. Confirm the vessel's panel shows blast pressure at 2.50 atm or more. Only a steam-driven Air
   Blower reaches that; a Twin-Tub Blower cannot.
3. Sneak and right-click the control to set Filling, and open the canal tap above.
4. Plain right-click for Normal once it holds as much iron as you want. It need not be full.
5. Wait for "Steel ready! Pour it out.", then sprint and right-click, held for a second, to pour.

## What goes wrong

**Blast pressure reads 0.00 atm with the boilers roaring.** The intake is a port, not a pipe. There
has to be a pipe in the cell in front of it, facing back at it. A blower bolted straight onto the
intake feeds nothing.

**Everything is built and nothing happens.** Read the panel top to bottom: it names an unbuilt
vessel, a misaligned intake, a misaligned transmission and missing power in that order, before it
says anything about the charge.

**The pressure sags part way through a blow.** The converter's air draw climbs with the blow rate.
Size the supply for the rate you mean to run; see [[Bessemer process]].

**The heat froze in the vessel.** A small residue, under a fifth of the vessel, chisels out of the
upper hatch once it has hardened. Anything larger means breaking the vessel, which returns all of
its construction materials and the metal as bits.

**Scrap will not go in.** Scrap is charged on the vessel's upper hatch, not on the control block,
and only before the blow.
