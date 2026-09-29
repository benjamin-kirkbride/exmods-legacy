---
title: Compatibility
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
order: 7
version: 0.7.0
---

What this mod is built to run beside. Every line below is in the 0.7.0 code. A mod not named here
has no adaptation and no guard, which says nothing either way about whether the pair works.

## What it needs

| requirement | value |
|---|---|
| Expanded Library | 0.8.3 or later |
| Steelmaking Expanded, if you run it | 0.10.0 |
| game | separate builds for 1.20, 1.21 and 1.22 |

Run the three Expanded mods as one set. Versions that have drifted apart are the usual cause of
blocks that vanish or refuse to place after an update, and there is a build of this mod for each
supported game version: download the one that matches your game rather than editing
`modinfo.json`, which produces a half-working install with items missing.

Expanded Library 0.8.3 refuses a Pipes and Power Expanded below 0.7.0 or a Steelmaking Expanded
below 0.10.0: the library logs one error and sends every joining player the line `exlib <version>
needs Pipes and Power Expanded 0.7.0 or later: update, or keep exlib 0.7.2 with the installed
versions.`, naming each mod that is behind.

Back up a world before moving it to this version. A world saved on 0.7.0 cannot go back to 0.6.9
or earlier: its pipes would lose their contents, its config would be back at the defaults, and
some pipes might not read their facing.

## Iron Industry Expanded and Steel Industry Expanded

These are the successors, new mods on the same library. Add either one to a world with this mod
and this mod and Steelmaking Expanded close in that world. The machines already built keep
working, and nothing new can be built from the two mods:

- their recipes are gone, and their blocks and items leave the creative inventory and the
  handbook, the guide pages included;
- their blocks drop none of their own items when broken; fuel, metal and the vanilla materials of
  a part-built machine still drop;
- their items are removed from each player's inventories on joining, from chests, racks, ground
  storage and other containers as their chunks load, and from the ground;
- a mechanical fluid pump, twin-tub blower or Bessemer converter already part-built can be
  finished: its pipe stage takes the new line's straight pipe;
- each player sees one chat notice the first time they join.

Some places are not swept, so an item left there stays: the inventories of boats, pack animals,
armour stands and traders; storage of other mods that is not a container block; items inside
another item, such as a bag in a chest; chunks that never load again. An item a machine hands out
later goes at the next sweep of the place it lands. Remove both new mods and the line opens again.

The two lines' pipes never join: a pipe of this mod beside a pipe of Iron Industry Expanded forms
two runs. A machine's port takes either line's pipe.

## Other mods

Nothing else in this mod adapts to or guards against another mod. There is no electricity anywhere
in the Expanded mods, so nothing here can be wired to another mod's grid. The ore-mod adaptations
(Expanded Matter, IndustrialStory) belong to Steelmaking Expanded and are listed on [its
compatibility page](/current/smex/mechanics/compatibility/).

## Reporting a conflict

A report that gets answered names the other mod and its version, says whether the game was single
player or a server, and carries the crash log or `server-main.log` rather than a video. For broken
audio, name the sound that is looping.
