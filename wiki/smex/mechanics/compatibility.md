---
title: Compatibility
covers:
  - "smex:hopperreinforced"
  - "smex:burden"
order: 8
version: 0.9.8
---

Compatibility questions are the most common thing asked on this mod's page, and the answers age
badly, so every line below carries the date it was true and where it comes from. "In the code"
means it is in 0.9.8 and can be relied on; a reported answer is somebody's experience on a version
that is now old.

## The other Expanded mods

| mod | version this needs | note |
|---|---|---|
| Expanded Library (exlib) | 0.7.2 | a hard dependency. This mod does not load at all against exlib 0.8 or later: the assembly reference no longer matches, and there is no guard, the loader simply skips it |
| Pipes and Power Expanded (ppex) | 0.6.7 or later (0.6.8 is the current release) | a hard dependency. The furnace, the stoves and the converter all breathe through its pipes |

Mismatched versions of these three is a real failure mode and not an obvious one: in August 2026 it
was twice the actual cause of "blocks missing after the update" reports. Update them together.

exlib 0.8 and later do not fail quietly. The library logs one error and sends every joining player
the line `exlib <version> does not work with Steelmaking Expanded and Pipes and Power Expanded:
keep exlib 0.7.2 with them, or replace them with Iron Industry Expanded.`

Two facts about exlib 0.7 that the mod page never answered: all three comments on it report clients
crashing when they look at rideable or interactable entities on 0.7.0, and all three report that
rolling back to 0.6.0 stops it (2026-06-23 to 2026-07-02). One reporter narrowed it to vanilla
boats with only the three Expanded mods loaded, so it is not a smex bug.

## What this mod patches

Collisions come from shared recipes, so this is the list to check another mod against.

- **Vanilla nugget crushing.** Limonite, magnetite, hematite and galena nuggets are patched to crush
  one for one instead of the vanilla average of 0.33. The patch is skipped entirely when Expanded
  Matter or IndustrialStory is loaded, because each owns nugget crushing outright. In the code.
- **Vanilla crushed ore.** A crushed-coke variant is declared, crushed iron smelts to an iron bloom
  at 20 to 1, and crushed stacks go to 128. In the code.
- **The vanilla tool mold, mold rack and coal pile** are extended by Harmony patch rather than
  replaced, so another mod that touches the same blocks can coexist. In the code.
- **ppex's pipe passthroughs and outlets** gain refractory tier variants and their recipes. In the
  code.

Crushing recipes are the specific danger. Two mods that both rewrite how a nugget crushes do not
merely duplicate each other; they can produce a loop where iron makes more iron.

## Named mods

| mod | what is known | when |
|---|---|---|
| Improved Metallurgy | Pick one, not both. The owner's position is that it implements the same mechanic a different way (2026-06-15); separately, running the two together crashed a whole server through duplicate nugget-crushing patches. There is no compat branch for it in the code | 2026-06 |
| Expanded Matter (`em`) | Works. Its crushed ores (`crushed-ore-hematite`, `-limonite`, `-magnetite`) are accepted as furnace feed, coke grinds into its powdered coal, and this mod stands back from vanilla nugget crushing while it is loaded. A failure means an out-of-date install of one of the two | in the code, confirmed by the owner 2026-06-16 |
| IndustrialStory | Supported, with one consequence to know: while it is loaded the furnace accepts `crushed-hematite` and `crushed-magnetite`, and `roasted-nugget-iron` and `roasted-crushed-iron` at the roasted premium, and it stops accepting vanilla `crushed-iron`, because that mod crushes iron bits into it and the pair would otherwise pay out more iron than went in. A load-order collision between the two crushing patches was reported in August 2026 | in the code; the report 2026-08-13 |
| Interesting Mining and Extraction | Unanswered. The community's own reading is that it would need a crusher recipe of its own. Nothing in the code knows about it | 2026-08 |
| Any mod adding tool molds | Any vanilla-style `toolmold-*` block works on a mold pedestal. Anvil and helve hammer molds are cast under a canal tap instead | owner, 2026-06 |
| Electricity mods | There is no electricity anywhere in the Expanded mods and no interop is planned | owner, 2026-07-19 |
| VS Director | Freezes the game when unbuilt ppex boilers are placed, diagnosed by a third party as an animation serialization problem. Unfixed | 2026-08-03 |
| Electrical Progressives | Its Advanced Wrench silently fails to complete a ppex boiler multiblock. Use the vanilla wrench | 2026-06-16 |
| Lumos, Wear and Tear, Real Smoke, Immersive Heat Sources, xskills | Asked about, never answered. Unanswered is not the same as incompatible | 2026-06 to 2026-08 |

## Game versions

Legacy game-version support was added on 2026-06-21. Before that the owner's position was that it
might work untested. Editing `modinfo.json` by hand to claim a version is not a supported fix and
produced a half-working install for the one player who tried it.

Whether the mods are safe to add to a world that has already been played for many hours has never
been answered by the owner. Nothing in the mod rewrites existing terrain; its ore feed comes from
ordinary vanilla ores.

## Reporting a crash

Two crashes near this mod's machines have been diagnosed, and in both cases the trigger was another
mod or corrupted saved state, not the furnace. The request that produced an answer both times was
the save file or `server-main.log`, so include one. A screenshot of a red message is not enough.
