---
title: Compatibility
covers:
  - "ppex:boilercornish-*"
  - "ppex:boilerlancashire-*"
order: 7
version: 0.6.9
---

What this mod is built to run beside. Every line below is in the 0.6.9 code. A mod not named here
has no adaptation and no guard, which says nothing either way about whether the pair works.

## What it needs

| requirement | value |
|---|---|
| Expanded Library | 0.7.2 |
| Steelmaking Expanded, if you run it | 0.9.9 |
| game | separate builds for 1.20, 1.21 and 1.22 |

Run the three Expanded mods as one set. Versions that have drifted apart are the usual cause of
blocks that vanish or refuse to place after an update, and there is a build of this mod for each
supported game version: download the one that matches your game rather than editing
`modinfo.json`, which produces a half-working install with items missing.

Expanded Library 0.8 and later refuse the pair: the library logs one error and sends every joining
player the line `exlib <version> does not work with Steelmaking Expanded and Pipes and Power
Expanded: keep exlib 0.7.2 with them, or replace them with Iron Industry Expanded.`

## Other mods

Nothing in this mod adapts to or guards against another mod. There is no electricity anywhere in
the Expanded mods, so nothing here can be wired to another mod's grid. The ore-mod adaptations
(Expanded Matter, IndustrialStory) belong to Steelmaking Expanded and are listed on
[its compatibility page](/current/smex/mechanics/compatibility/).

## Reporting a conflict

A report that gets answered names the other mod and its version, says whether the game was single
player or a server, and carries the crash log or `server-main.log` rather than a video. For broken
audio, name the sound that is looping.
