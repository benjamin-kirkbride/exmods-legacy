---
title: Blast furnace
version: 0.9.8
---

The [[Blast Furnace Door]] starts this build and is the block you work from afterwards: it
carries the panel, and the charge is lit through it. What the furnace does with burden, blast
and heat is on [[Ironmaking]].

## The shape of it

The furnace is a hollow tower nine blocks tall and six by five on the ground, with three of those
levels below the door you stand at. Dig the hole before you start: the hearth floor sits three
levels down, and the two hoppers stand five levels up.

Reading it from the bottom:

| level, from the door | holds |
|---|---|
| three below | the hearth floor |
| two below | the two Tuyeres, front and back, and the iron tap on one side wall |
| one below | the slag tap on the opposite side wall |
| the door | the charging door, the block you stand at |
| one and two above | shaft |
| three above | the two gas outlets, front and back |
| four above | the Bell Hopper, over the shaft |
| five above | the Reinforced Hopper, on top of it |

The hollow inside is left empty. The hoppers fill it with burden; that is what the air cells in the
outline are for.

## Build order

1. Clear the volume and stand where you want to work from. The door is the control block and
   everything else is placed relative to it, so its facing fixes the whole furnace. It cannot be
   turned afterwards.
2. Place the door, then hold ctrl and shift and right-click it. That raises the build outline and
   prints the list of everything still missing, by name and count, in chat. Red cells hold the
   wrong block. The outline only shows while the structure is incomplete.
3. Lay the hearth floor three levels down, then build up course by course. Any refractory brick
   tier is accepted; you can mix them.
4. Set the two tuyeres facing into the shaft, two levels below the door, one at the front and one
   at the back, and leave the cell in front of each one free for its pipe.
5. Set the two taps on the side walls at the middle of the shaft: the iron tap at tuyere level, the
   slag tap one level higher on the other side. Leave the cell below each spout free for a molten
   canal start.
6. Cap the shaft with the two gas outlets at the third level above the door, front and back, again
   with room outside each for pipe.
7. Stack the bell hopper over the shaft and the reinforced hopper on top of it.
8. The outline disappears and the door says `Blast Furnace structure is complete!` when it is
   done.

The tuyeres are on the front-and-back axis at the bottom and the gas outlets on the same axis at
the top, so the air line and the flue line run along the same side of the building. The taps are on
the two sides, which is where the canals go. Plan the room around that: a furnace with its taps
against a cliff is a furnace you cannot drain.

## The first light

The sequence matters more than anything else on this page. Air first, fire second.

1. Stock the reinforced hopper: iron in the four iron slots, coke or charcoal in the two fuel
   slots, lime in the two flux slots.
2. Let the bell hopper fill the shaft. The door reads `Burden loaded: 320 / 320` when there is
   enough. Ctrl and right-click on the Reinforced Hopper stops and starts the dropping.
3. Start the blowers. Look at the door: it has to show a blast pressure at or over 1.50 atm before
   you light anything.
4. Open the door, light the burden with a torch, close the door. Every pile has to catch; until
   they all have, the door says "Piles are partially lit. Waiting for the fire to spread..."
5. The state line goes to Firing. The hearth climbs about 4 C per second on cold blast and reaches
   iron's melting point in around two and a half minutes.
6. At 1482 C the state goes to Melting and the molten iron figure starts to rise.

## The first tap

1. Place a molten canal start in the cell below the iron tap's spout, and run canal out from it to
   wherever you mean the metal to go. A pedestal with a mold, a canal tap over a barrel, anything
   that is a destination.
2. Right-click the tap with an empty hand. Nothing in your hand, or it will not toggle. Without a
   canal start below it, it refuses with "No canal start found below the tap!".
3. Do the same for the slag tap on the other side when the slag figure climbs; a full slag
   reservoir stalls the melt just as a full iron one does.

Both taps drain 40 units per second, which is faster than the furnace melts, so they are opened as
needed rather than left open.

## What goes wrong

**Lighting before the blowers are running.** A hearth with piles alight and no blast at 1.5 atm
gives ten seconds and then goes out. This is the single most common failure with this build. The
burden is not spoiled; start the blowers and light it again.

**Reading the burden line as proof of air.** `Burden loaded: 40 / 320` counts the charge in the
shaft. The two blast lines under it are the air, and they only appear once something is burning.

**A tap with nothing under it.** Metal does not back up and wait. It pours into whatever is below,
and if that is a start block with nowhere to go, the start fills and then freezes.

**Building it into a hillside.** The furnace needs its front and back for pipe and both sides for
canals, and the hopper on top needs to be reachable.

**A snow layer where an air cell should be.** Snow is not air; it fails the cell.

**An old furnace that will not take the lower pressure.** The gate dropped from 2.5 atm to 1.5 atm
on 2026-08-13, and a furnace built before that keeps the old figure until its door is broken and
replaced. Tier 3 refractory was the only accepted brick until late August 2026; a furnace built
then still stands, and a new one takes any tier.
