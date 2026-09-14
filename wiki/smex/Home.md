# Steelmaking Expanded (smex)

Steelmaking Expanded adds an industrial iron and steel line on top of vanilla metalworking: a blast
furnace that smelts ore into a reservoir of molten metal, the machinery that blows and preheats its
air, a network of canals that plumbs the melt to molds, and a Bessemer converter that turns the
iron into steel. This is version 0.9.8, and it needs Expanded Library 0.7.2 and
[Pipes and Power Expanded](/current/ppex/Home/) 0.6.8. It carries no guard against later library
versions: with exlib 0.8 or newer installed the game simply does not load this mod, because its
assembly reference no longer matches.

The line has two halves. Iron comes out of the [[Blast furnace]], a tall refractory tower fed by a
pair of hoppers that combine ore, fuel and lime into burden, blown through two tuyeres and tapped
into a [[Molten metal]] network of canals, taps and barrels that carries the melt to a pedestal for
[[Casting]]. [[Hot blast]] is the upgrade to that half: a pair of cowper stoves reclaim the heat
from the furnace's own exhaust and hand it back to the air, which roughly triples the melt rate,
with a smoke stack venting the surplus. Steel is the second half: the [[Bessemer process]] blows
air through a bath of molten iron to burn the carbon out of it, and it is the most demanding
machine here, wanting mechanical power and a blast pressure only a steam-driven blower reaches.
Nothing is wasted at the end of it either, since [[Slag and its products]] turns the furnace's
by-product into fertilizer, mortar and paving.

Every gameplay number in the mod is a config value you can read and change in place; see
[[Commands and config]] for the keys, the defaults and the chat commands, and [[Compatibility]] for
what this mod patches and which other mods it collides with. If you are starting from nothing, work
through [Getting started](Getting-started) in order; if something is already broken, the
[FAQ](FAQ) collects the questions players actually asked. The in-game handbook articles are still
shipped and are grouped under Handbook in the sidebar, but the pages here are the current ones.
Steelmaking Expanded itself is not being carried forward: its successor is Steel Industry Expanded,
a new mod built on exlib 0.8 and later rather than an update to this one, and worlds do not carry
over between the two.
