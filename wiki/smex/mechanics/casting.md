---
title: Casting
covers:
  - "smex:toolmold*"
  - "smex:moltencanal-moldpedestal*"
  - "smex:moltencanal-tap*"
version: 0.9.8
---

Casting is where the melt becomes items. Molten metal fills a mold from the canal run, the cast
cools in place, and a plain right-click hands over the finished piece. The rules that catch people
are the two either side of that: a mold under an open pour does not cool at all, and a mold holding
liquid metal burns you and spills the moment it leaves your hand.

## The molds

This mod adds three ceramic molds, clay-formed and fired like any vanilla tool mold.

| mold | units to fill | yields |
|---|---|---|
| plate ceramic mold | 200 | 1 metal plate |
| double ingot ceramic mold | 200 | 2 ingots |
| quad rod ceramic mold | 400 | 4 rods |

Vanilla tool molds work too: any `toolmold-*` block sits on a pedestal and takes 100 units unless
its own definition says otherwise. The two large molds, the anvil and the helve hammer, do not fit
a pedestal; they go under a Molten Canal (Tap) instead, and the game says "This mold is used with
the mold pedestal." or "This mold will not fit on the pedestal." when you try the wrong one.

An admin can switch any of the three added molds off; see [[Commands and config]]. A disabled mold
loses its clay-forming recipe on the next world load and yields nothing from then on, even if it
is already placed and full.

## Filling a mold

On a Molten Canal (Mold Pedestal):

1. Sneak and right-click with the mold in hand to place it. Sneak and right-click again to take it
   back.
2. Sprint and right-click to start the pour. The pedestal severs itself from the run while it is
   shut, so nothing fills behind a closed pedestal.
3. The mold fills from the pedestal's own cell, which fills from the run, until it is full or the
   run runs dry.

On a Molten Canal (Tap), for a barrel or one of the large molds, the keys are the same: sneak and
right-click to park or take the barrel or mold, sprint and right-click to toggle the pour. The tap
drains 20 units per second.

## Cooling

Every unit that lands in a mold arrives at the temperature of the cell feeding it and restamps the
whole cast to that temperature. A mold under an open pour therefore does not cool: it sits at the
canal's temperature until it is full, until the run empties, or until you shut the pour. Only then
does the clock start.

Cooling itself runs on the item stack, so it keeps running whether the mold is on a pedestal, under
a tap, in your hand or on the ground. A cast is finished when it is hardened, below 30 per cent of
the metal's melting point: 445 C for iron, 451 C for steel.

That is the whole of the answer to why a canal-fed cast takes so much longer than a crucible pour.
The crucible pours a fixed amount of metal at crucible temperature and stops; the canal keeps
topping the mold up at furnace temperature. Players measured about 3 minutes for a crucible-poured
mold against around 70 real minutes for a canal-fed one in August 2026, which is why a run of any
size needs its molds staged in advance: count them before you tap, not after. Taking a mold off its
pedestal and setting it on the ground takes it out of the pour and gets it cooling.

## Getting the casting out

- **Full and hardened:** right-click with an empty hand. You get the plate, ingots or rods and the
  empty mold stays where it is.
- **Anything else:** right-click picks up the mold itself with its contents. A mold holding liquid
  metal comes up only into an empty active hand, and the game says "The metal is still liquid! Take
  the mold with an empty hand." otherwise.

## Carrying a filled mold

Two rules, both enforced once a second on the server:

1. **Metal above 200 C burns you** at 1 health per second while you hold the mold, unless you are
   wearing heavy leather gloves or a blacksmith's gloves. Tongs do not help and were rejected by
   design, since a mold is held with both hands.
2. **Liquid metal only rides in your active hand.** A mold with liquid metal in any other slot, a
   backpack, a chest or a mold rack loses its contents at once: "The molten metal spilled out of
   the mold!" Switching to another hotbar slot spills the one you just left.

A cast that has cooled past liquid is safe to stash; the spill rule only looks at metal that is
still liquid, and the burn rule only at metal over 200 C.

## What goes wrong

**The mold burned me.** No gloves. Heavy leather gloves or a blacksmith's gloves, in the hands
slot.

**The metal vanished out of the mold.** It was in a slot other than the active one, on a rack, or
in a container. Carry a liquid mold in your hand and set it down.

**The mold will not go on the pedestal.** It is an anvil or helve hammer mold, which is cast under
a canal tap.

**The mold sits under the tap but never fills.** The pour is off, or the tap's own cell has frozen.
A tap keeps filling a barrel whose contents have set, but it stops filling a mold that has
hardened, because a hardened mold is a finished cast.

**Right-clicking gives me the mold instead of the casting.** The cast is not both full and
hardened yet, or the mold type has been disabled on the server.

## Dated note

Ceramic molds are the current route. The owner said in July 2026 that they are to be replaced by
sand and cast iron molds in a later mod; nothing in 0.9.8 has changed yet.
