---
title: Molten metal
covers:
  - "smex:moltencanal-start*"
  - "smex:moltencanal-straight*"
  - "smex:moltencanal-bend*"
  - "smex:moltencanal-tjunction*"
  - "smex:moltencanal-xjunction*"
  - "smex:moltencanal-tap*"
  - "smex:moltencanal-moldpedestal*"
  - "smex:moltenbarrel"
  - "smex:blastfurnacetap*"
order: 3
version: 0.9.8
---

Molten metal is plumbed, not carried. A run of canal blocks takes iron or slag from a furnace tap
and levels it out toward wherever you have opened a drain: a mold pedestal, a canal tap over a
barrel or a large mold, or the Bessemer process. Metal in the canals is cooling the whole time,
and a cell that cools too far sets solid and blocks the run until it is chipped out.

## How a run flows

Every canal block is a cell that owns its own metal, its type and its temperature. A run is
anchored by one or more Molten Canal (Start) blocks, and each tick the network works out how far
every cell is from the nearest start and moves metal along each connection:

- Metal running away from the start, or into a drain fitting, moves as a whole: the giving cell
  hands over its entire surplus.
- Metal levelling back toward the start, or between two cells the same distance out, moves half
  the difference, so an idle run settles instead of sloshing.
- At most 100 units cross one connection per second, and a gap under 1 unit is left alone.
- Two different metals sit side by side and never mix. A junction fills its first exit that is not
  already full.

There is no distance limit. A run reaches as far as you feed it and as far as the metal stays
liquid; length costs nothing on its own.

| fitting | holds |
|---|---|
| Molten Canal (Start) | 200 units |
| straight, bend, T-junction, X-junction | 100 units |
| Molten Canal (Tap), Molten Canal (Mold Pedestal) | 50 units, plus whatever is parked under them |
| Molten Barrel | 800 units |

Drain rates: a furnace tap pours 40 units per second, a Molten Canal (Tap) drains 20 units per
second into the barrel or large mold parked under it, and a pedestal drains as fast as its cell
fills.

A closed tap or a pedestal that is not pouring severs the run at its own cell, so a shut fitting
does not fill up behind your back. So does a frozen cell.

## Temperature

Each metal has its own melting point, and three thresholds follow from it.

| metal | shows as liquid above | cell sets solid below | chiselable below |
|---|---|---|---|
| iron | 1186 C | 1482 C | 445 C |
| steel | 1202 C | 1502 C | 451 C |
| slag | 576 C | 720 C | 216 C |

The first column is 80 per cent of the melting point, and it governs what things look like and
what you may carry: a cell above it reads Liquid and below it reads Cooling, and metal under it no
longer spills out of a mold in a bag or a chest. It is not a flow limit. Metal between the first
two columns is still running through the canals, so a canal full of iron at 1300 C is working
normally.

A cell with metal in it that falls under the melting point latches solid: it stops passing metal,
drops off the run and shows "Solidified!". It cannot be chipped out until it falls under the
chiselable threshold, which is what "Wait for the metal to harden!" means. Chip it out with a
chisel in hand and a hammer in the off-hand; you get one metal bit back per 5 units, and the cell
rejoins the run.

Metal already in a cell is re-heated by metal poured over it, which is why a fitting under a
running tap stays liquid even when it is brim full. A run that is fed keeps itself molten; a run
left standing does not. Empty a run before you shut the furnace down.

## Tapping the furnace

1. Place a Molten Canal (Start) below the tap's spout, one block out from the face the tap points
   at. Without it the tap refuses to open.
2. Right-click the tap with an empty hand to open it. The lower tap pours iron, the upper pours
   slag.
3. Lay the run out from the start with straights, bends and junctions, and put the drains where
   you want the metal: pedestals for small molds, a canal tap for a barrel or a large mold, the
   converter's input tap for steel.

Metal does not back up and wait. A tap with nothing under it pours onto a start that fills and
then freezes, and a junction fills the first exit it finds, not the one you meant. Open only the
paths you want fed.

A canal start also accepts a hand pour from a crucible, which is a practical way to feed a short
run without a furnace.

## Sealing a canal by hand

An empty straight canal can be capped into a manual valve: right-click it holding at least
4 fire clay and it seals, severing the network at that block. A chisel breaks the seal and returns
2 fire clay. Only a drained section seals, and its connected neighbours have to be empty too, so a
seal can never trap metal against itself: otherwise it refuses with "Cannot seal while there is
metal!".

## The barrel

A Molten Barrel holds 800 units and sits under a Molten Canal (Tap), which fills it at 20 units
per second. A barrel takes metal even when its contents have already set, remelting them. It is
bulk storage and a heat store, not a dispenser: metal does not come back out of a barrel into the
canals.

To get the metal back, let it harden and chisel it out with a chisel and hammer, at the usual
5 units per bit. Do not break a barrel that is both full and hardened: breaking one returns the
barrel and nothing else. A barrel broken while it still holds hot or partial contents does drop
its metal as bits.

## What goes wrong

**The metal stops after a few blocks.** Check the cells: the first one reading "Solidified!" is
where the run froze, and everything past it has dropped off the network. A four-block stall was a
real regression in August 2026, fixed in 0.9.7 along with a doubling of canal capacity and
throughput, so an install from 0.9.7 on is not the one that had it.

**A tap will not open.** There is no canal start under its spout, or the block under the spout is
a straight instead of a start.

**Metal went the wrong way at a junction.** Junctions fill the first exit that is not full. Seal
the branch you do not want, or do not build it open.

**A solidified cell will not chisel.** It is still above the chiselable threshold. Look at it: it
says "Wait until hardened for chiselling." until it is ready, then "Hardened! Chisel out the
metal."

**The start block froze with metal in it.** It chips out like any other cell. This is the
chisel-and-hammer interaction that recovers the metal, not the vanilla chiselling that reshapes a
block: the canal shapes themselves cannot be carved.

**Everything solidified overnight.** A standing run cools. Drain the canals into molds or a barrel
before you stop feeding them, and keep runs as short as the layout allows.
