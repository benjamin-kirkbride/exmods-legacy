---
title: Compatibility
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
order: 7
version: 0.6.8
---

What is known about running this mod beside other mods, as of 2026-09-14. A mod that is not on this
list has never been reported either way. An entry with no answer is an entry with no answer: it is
not a statement that the pair is broken.

## What it needs

| requirement | value |
|---|---|
| Expanded Library | 0.7.2 |
| Steelmaking Expanded, if you run it | 0.9.8 |
| game | separate builds for 1.20, 1.21 and 1.22 |

Run the three Expanded mods as one set. Versions that have drifted apart are the usual cause of
blocks that vanish or refuse to place after an update, and there is a build of this mod for each
supported game version: download the one that matches your game rather than editing `modinfo.json`,
which produces a half-working install with items missing.

Expanded Library 0.7.0 has three unanswered crash reports against it, all the same shape: the
client crashes when it looks at a rideable or interactable entity, vanilla boats included, and
rolling back to 0.6.0 stops it. It was reported in June and July 2026 with only the three Expanded
mods loaded, so it is not a fault in this mod, and nothing in the record says which exlib release
fixes it. The [Expanded Library page](/current/exlib/Home/) carries what is known.

## Other mods

| mod | as of | what is known |
|---|---|---|
| any electricity mod | 2026-07-19 | no interoperation of any kind. There is no electricity anywhere in the Expanded mods, so nothing here can be wired to another mod's grid |
| VS Director | 2026-08-03 | placing an unbuilt boiler freezes the game. Diagnosed by a third party as an animation serialization problem on that mod's side. No fix recorded |
| Electrical Progressives | 2026-06-16 | its Advanced Wrench silently fails to complete a boiler multiblock, with no error. Use the vanilla wrench |
| Lumos | 2026-09-14 | raised, never diagnosed. No known conflict |
| Wear and Tear | 2026-09-14 | raised, never diagnosed. No known conflict |
| Real Smoke | 2026-09-14 | raised, never diagnosed. No known conflict |
| Immersive Heat Sources | 2026-09-14 | raised, never diagnosed. No known conflict |
| xskills | 2026-09-14 | raised, never diagnosed. No known conflict |
| Improved Metallurgy | 2026-06-15 | an ore and metallurgy overlap, and the dangerous kind: running both crashed a server through duplicate nugget-crushing patches. Pick one |
| industrialstory | 2026-08-13 | the same class of collision. Load order decided which mod's crushing patches worked, and the pair opened an iron duplication loop |
| Expanded Matter | 2026-06-16 | its crushed ores work. A failure means an out-of-date install of one of the two |
| Interesting Mining and Extraction | 2026-08-15 | never answered. The community's own reading was that it would need a crusher recipe written for it |

The last four are ore and crushing mods and belong to Steelmaking Expanded rather than to pipes and
power; they are listed here because most people run both. The rule they teach is worth stating
plainly: a mod that adds the same thing is a choice, while a mod that patches the same vanilla
recipe is a hazard, and crushing recipes are where these mods collide.

## Reporting a conflict

A report that gets answered names the other mod and its version, says whether the game was single
player or a server, and carries the crash log or `server-main.log` rather than a video. For broken
audio, name the sound that is looping. In both diagnosed crashes above, the save file or the log
was what produced the answer.
