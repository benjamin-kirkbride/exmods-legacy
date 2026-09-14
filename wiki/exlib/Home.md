# Expanded Library (exlib)

Expanded Library is the shared framework mod behind Pipes and Power Expanded and Steelmaking
Expanded. It ships no gameplay content of its own - install it because another mod depends on it.
Current version is 0.7.2.

It provides the block-network framework that both dependent mods build their pipe and molten
networks on, the multiblock structure system their boilers, engines, furnaces and converters are
built from, the production-machine base every ticking machine shares, block-entity healing that
recovers state lost to a desync or a failed chunk load, block migrations that rewrite renamed
block codes in old saves, and the registries and shared helpers (attribute-driven block/item/
command registration, versioned live-editable config, per-mod recipe-cost profiles, per-player
display preferences) the other two mods are written against.

0.7.2 is the last version of exlib in this line. 0.8 and later serve Iron Industry Expanded and
Steel Industry Expanded instead. ppex 0.6.8 and smex 0.9.8 carry no guard against later exlib
versions; the game's loader simply fails to load them at all once exlib 0.8 or later replaces
0.7.2, so keep exlib 0.7.2 installed alongside them. exlib 0.8 itself logs one error naming the
clash and repeats it to every joining player.

exlib has no in-game handbook of its own; the handbook articles that use its framework live in
ppex and smex.
