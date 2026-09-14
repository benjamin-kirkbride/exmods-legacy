# Pipes and Power Expanded: frequently asked questions

Questions that pipes and power alone answer. For questions that need Steelmaking Expanded too, see
the [Steelmaking Expanded FAQ](../smex/FAQ.md).

## These steam engines are very weak, is it even worth the iron?

The game has no single power number: an engine's output is torque, resistance and rotation speed
together. A helve hammer resists about 0.175, a Watt engine drives about 0.3, the three Cornish
throttle settings are 0.2, 0.4 and 0.8, and a waterwheel is about 9, roughly 36 helve hammers worth.
A steam engine is deliberately weaker than a waterwheel; its advantage is that you can place it
anywhere and run it on demand, not that it wins on raw output. One engine drives one attached
sub-machine, so a setup that needs both a pump and a blower needs a second engine and boiler. See
[Steam Power: Engines](handbook/engines/).

## Which way round does the valve go, and why does a closed pipe still vent?

The pressure valve is directional: the copper-trimmed side is the input. Opening any valve
reconnects the two networks either side of it, and pressure equalizes across the join, which is why
a run can read one flat pressure even with the tuyeres still drawing air; the valve does not block
that flow in either direction. A closed valve is not sealed until it has been opened and closed once
after placing, and a pipe run left with no valve at all keeps venting through the open end. See
[Steam Power: Fittings](handbook/fittings/).

## My boilers exploded

For the ordinary case: a boiler makes 16 L/s of exhaust and an open pipe only carries 8 L/s, so
without a chimney block or a route to a smoke stack the boiler pressurises past its safety valve.
Opening the lid dumps pressure fast in an emergency. There is a second, unresolved case: boilers
found at critical pressure right after a chunk reload, relog or server restart, with a safety valve
fitted and the hatch open. No fix for that case exists; the mitigation players actually use is to
vent the boiler and stop the run before a restart, not to trust the safety valve to cover it. See
[the boiler, engine and pipe line](Instructions.md#the-boiler-engine-and-pipe-line).

## It just says missing 6x and does not tell me what

A right-click-constructed multiblock will not place without room for its whole footprint: Cornish
boilers are 3x2x4, Lancashire boilers 3x2x6, and the placement overlay shows which cells are missing.
A snow layer is not air and will silently block a cell of an otherwise complete structure. Not every
third-party wrench works either: Electrical Progressives' Advanced Wrench fails to complete a boiler
multiblock without any error. See [the boiler, engine and pipe line](Instructions.md#the-boiler-engine-and-pipe-line).

## Is the mechanical power generator meant to sound like a ton of bells clicking?

No: reusing the planetary-gear sound for a working machine was a mistake, and the block is due to be
replaced by a crankshaft. The Cornish engine's clank is by design, the piston hitting the bottom of
the cylinder. The more serious half of this is an audio leak: looping machine sounds can fill
OpenAL's 250-voice limit and break all game audio, not just this mod's; a report of broken sound
needs the sound's name, not a video, because the leak has recurred from more than one source. See
[Steam Power: Engines](handbook/engines/).

## The recipe is invalid and I cannot craft it

The wooden axle recipe shipped with the wrong orientation for a while; it was reported, fixed, and
then the same defect reappeared on a later version before being confirmed fixed for good on
2026-06-22. If a recipe still looks invalid, check your version against the changelog before
assuming it is a new bug.

## Would it work with <some other mod>?

There is no electricity anywhere in these mods, so nothing wired to another mod's electrical system
interoperates with them. Two real conflicts are diagnosed: VS Director freezes the game when an
unbuilt boiler is placed, an animation-serialization problem in that mod; Electrical Progressives'
Advanced Wrench silently fails to complete a boiler multiblock. Reports against Lumos, Wear and
Tear, Real Smoke, Immersive Heat Sources and xskills exist with no diagnosis either way, so an
unanswered report is not the same as a known incompatibility.

## It renders for the player who built it, and the server restart eats the rest

Every one of these is state rebuilt on chunk load, not a separate bug per symptom: a multiblock that
looks complete only to its builder until a restart, pipes that implode on restart with their
machines off, and a login pressure spike bursting pipes and boilers that have overflow valves
fitted. None of these is fixed. There is nothing to do about it in the moment beyond what the boiler
explosion question already says: vent and stop a pressurised run before a restart, rather than
trusting it to survive one. See [the boiler, engine and pipe line](Instructions.md#the-boiler-engine-and-pipe-line).

## How do I change the numbers this mod uses?

`/exmod config` and `/exmod recipes` are Expanded Library's own commands, not this mod's; the mod id
is the section name, so tuning ppex is `/exmod config ppex ...` and its recipe costs are
`/exmod recipes ppex ...`. Gameplay tunables live in `ModConfig/ppex_values.json` and construction
costs in `ModConfig/ppex_recipes.json`, both editable live through those commands.
