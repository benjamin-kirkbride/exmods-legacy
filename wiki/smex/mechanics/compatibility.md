---
title: Compatibility
covers:
  - "smex:hopperreinforced"
  - "smex:burden"
order: 8
version: 0.9.9
---

What this mod is built to run beside. Every line below is in the 0.9.9 code. A mod not named here
has no adaptation and no guard, which says nothing either way about whether the pair works.

## The other Expanded mods

| mod | version this needs | note |
|---|---|---|
| Expanded Library (exlib) | 0.7.2 | a hard dependency. exlib 0.8 and later refuse the pair: the library logs one error and sends every joining player the line `exlib <version> does not work with Steelmaking Expanded and Pipes and Power Expanded: keep exlib 0.7.2 with them, or replace them with Iron Industry Expanded.` |
| Pipes and Power Expanded (ppex) | 0.6.9 or later | a hard dependency. The furnace, the stoves and the converter all breathe through its pipes |
| game | 1.20, 1.21 and 1.22 | a separate build per game version; download the one that matches rather than editing `modinfo.json` |

Update the three mods together. Versions that have drifted apart are the usual cause of blocks
missing after an update.

## What this mod patches

Collisions come from shared recipes, so this is the list to check another mod against.

- **Vanilla nugget crushing.** Limonite, magnetite, hematite and galena nuggets crush one for one
  instead of the vanilla average of 0.33. The patch is skipped entirely when Expanded Matter or
  IndustrialStory is loaded, because each owns nugget crushing outright.
- **Vanilla crushed ore.** A crushed-coke variant is declared, crushed iron smelts to an iron bloom
  at 20 to 1, and crushed stacks go to 128.
- **The vanilla tool mold, mold rack and coal pile** are extended by Harmony patch rather than
  replaced, so another mod that touches the same blocks can coexist.
- **ppex's pipe passthroughs and outlets** gain refractory tier variants and their recipes.

Two mods that both rewrite how a nugget crushes do not merely duplicate each other; they can
produce a loop where iron makes more iron. That is the collision to look for in any ore mod.

## Mods the code adapts to

| mod | what the code does |
|---|---|
| Expanded Matter (`em`) | its crushed ores (`crushed-ore-hematite`, `-limonite`, `-magnetite`) are accepted as furnace feed, coke grinds into its powdered coal, and this mod stands back from vanilla nugget crushing while it is loaded |
| IndustrialStory | the furnace accepts `crushed-hematite` and `crushed-magnetite`, and `roasted-nugget-iron` and `roasted-crushed-iron` at the roasted premium, and stops accepting vanilla `crushed-iron`, because that mod crushes iron bits into it and the pair would otherwise pay out more iron than went in. This mod stands back from vanilla nugget crushing while it is loaded |
| any mod adding tool molds | any vanilla-style `toolmold-*` block works on a mold pedestal. Anvil and helve hammer molds are cast under a canal tap instead |

## Reporting a conflict

Name the other mod and its version, say whether the game was single player or a server, and attach
the save file or `server-main.log`. A screenshot of a red message is not enough.
