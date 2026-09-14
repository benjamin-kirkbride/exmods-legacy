---
title: Slag and its products
covers:
  - "smex:slag"
  - "smex:powderedslag"
  - "smex:slagpath*"
  - "smex:slagpathslab*"
  - "smex:slagpathstairs*"
  - "smex:solidifiediron"
version: 0.9.8
---

Every melt cycle in the blast furnace makes 102 units of molten iron and 17 units of molten slag,
so the furnace produces one unit of slag for every six of iron. Slag is a second liquid on the same
molten network, and it is worth handling rather than dumping: ground up it is a phosphate
fertilizer and the bulk of a mortar recipe, and whole it paves roads.

## Getting slag out of the furnace

The hearth holds 1200 units of slag, and a full slag reservoir stalls the melt exactly as a full
iron reservoir does. The upper Molten Metal Tap, the one on the opposite side from the iron tap,
drains it at 40 units per second into a canal start below its spout.

Slag runs the canals like any other metal, with its own thresholds:

| | slag |
|---|---|
| liquid above | 576 C |
| freezes below | 720 C |
| chiselable below | 216 C |

Keep it on its own run, or at least never let it meet iron in the same cell: two metals do not mix,
and a cell holding one refuses the other.

Slag is not castable, so molds are not the route. Run it into a Molten Barrel or a dead-end of
canal, let it set, and chip it out with a chisel in hand and a hammer in the off-hand. That gives
one Slag item per 5 units, the same rate as every other recovery in the mod.

The Solidified Slag block gives back the slag it holds when broken, at 80 to 100 per cent of its
stored count. Nothing in 0.9.8 places one; slag comes out of the canals and the barrel.

## What slag is for

| product | made from |
|---|---|
| Powdered Slag | grind Slag in a quern, one for one |
| 4 mortar | 1 L of slaked lime and 8 Powdered Slag in a barrel |
| 2 Slag Path | 4 Slag and gravel |
| 1 Slag Path Slab | 2 Slag and gravel |
| 1 Slag Path Stairs | 4 Slag and gravel |

Powdered Slag is a phosphate fertilizer: 20 P and 5 K, no nitrogen, with a permanent boost of 5 P.
It dissolves in water, so it goes on the soil, not in a barrel of anything.

## Scrap

Scrap is the other thing that comes back off the line, and it has exactly one destination.

- A furnace that goes out leaves its molten iron as two Solidified Iron blocks in the hearth. Mine
  them for one iron bit per 5 units of iron that was in the reservoir. The slag it was holding is
  lost.
- A frozen canal cell, a set barrel or a frozen converter heat all chip out at the same 5 units per
  bit.
- Iron and steel bits go back into the Bessemer Converter as cold scrap, at 5 units each. The
  [[Bessemer process]] page has the arithmetic of how much a given blast pressure can carry.

The blast furnace takes no scrap at all, and that is deliberate: a route that turned finished iron
back into furnace feed would pay out more metal than it consumed. If a mod adds a way to crush
metal bits into crushed iron ore, the furnace's own feed list may stop accepting vanilla crushed
iron; see [[Compatibility]].
