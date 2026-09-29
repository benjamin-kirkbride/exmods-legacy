# Expanded Library (exlib)

Expanded Library is the shared framework mod behind Pipes and Power Expanded and Steelmaking
Expanded. It ships no gameplay content of its own - install it because another mod depends on it.
Pipes and Power Expanded 0.7.0 and Steelmaking Expanded 0.10.0 need exlib 0.8.3 or later.

It provides the block-network framework that both dependent mods build their pipe and molten
networks on, the multiblock structure system their boilers, engines, furnaces and converters are
built from, the production-machine base every ticking machine shares, block-entity healing that
recovers state lost to a desync or a failed chunk load, block migrations that rewrite renamed
block codes in old saves, and the registries and shared helpers (attribute-driven block/item/
command registration, versioned live-editable config, per-mod recipe-cost profiles, per-player
display preferences) the other two mods are written against.

The same exlib serves Iron Industry Expanded and Steel Industry Expanded. It refuses ppex below
0.7.0 and smex below 0.10.0: it logs one error naming them and repeats it to every joining player,
so update the three together. The older ppex and smex ran on exlib 0.7.2, which stays on the
mod database for worlds that keep them.

exlib has no in-game handbook of its own; the handbook articles that use its framework live in
ppex and smex.
