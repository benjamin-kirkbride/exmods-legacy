# Pipes and Power Expanded: frequently asked questions

Questions that pipes and power alone answer. For questions that need Steelmaking Expanded too, see
the [Steelmaking Expanded FAQ](/current/smex/FAQ/).

## Is there a complete list of parts I need to make?

[[Getting started with steam power]] carries one: everything a first plant needs, in the order to
build it, with the materials each right-click construction stage consumes. The build pages for the
Cornish Boiler and the Lancashire Boiler carry their own materials tables, computed from the
structure itself.

## These steam engines are very weak, is it even worth the iron?

The game has no single power number: an engine's output is torque, resistance and rotation speed
together. A Watt Engine delivers 0.3 and holds a shaft load of 0.6; the Cornish Engine's three
throttle settings deliver 0.2, 0.4 and 0.8 and hold 0.4, 0.8 and 1.6. A vanilla helve hammer
resists 0.125, so a Watt pulls about five of them and a Cornish on high about thirteen. The 0.6.6
rebalance raised the load an engine holds per unit of power so that an engine out-pulls a vanilla
waterwheel, which bogs down under load rather than stopping. What steam buys beyond the pull is
placement and power on demand: anywhere, from a fire rather than a season, and driving things no
waterwheel can, starting with the air blowers of Steelmaking Expanded. One engine drives one
sub-machine, so a plant that needs a pump and a blower needs two engines. See Steam engines.

## Which way round does the valve go, and why does a closed pipe still vent?

The Piping (Pressure Valve) is directional and the copper-trimmed side is the input; a wrench
flips it. A plain Piping (Valve) has no direction: open, the run flows through it, and closed, it
severs the network at its own cell. Opening one merges the two sides into a single pool that
settles at one pressure at once, which looks like the pressure has been dumped and is really just
the same gas in more pipe. And a closed valve does not cap the far side: if nothing is built past
it, that face is an open end for the run beyond and bleeds 8 L/s like any other hole. See
[[Valves and outlets]].

## My boilers exploded

A boiler explodes when three things hold together for 30 continuous seconds: it is boiling, the
fire is lit, and its internal pressure is at or above the choke pressure with the lid shut.
Opening the lid dumps 200 L/s and resets that timer. Pumped feedwater above 1 atm flashes to extra
steam and is the common way to drive a vessel there.

Venting is the ordinary reason a boiler dies, and it is not this. A boiler makes 16 L/s of exhaust
and an open pipe end only carries 8 L/s, so without a chimney on the exhaust outlet or a route to
a smoke stack the boiler counts as choked and the fuel pile is snuffed 10 s later. A choked flue
puts the fire out; it does not burst the vessel.

There is a second case that is still open: boilers found at critical pressure immediately after a
chunk reload, a relog or a server restart, with a relief valve fitted and the hatch open. No fix
exists as of 2026-09-14, and the only mitigation players use is to vent the boiler and stop the
run before a restart rather than trusting the valve to cover it. See [[Boilers]].

## It just says missing 6x and does not tell me what

It does now: hold ctrl and shift and right-click the machine and the chat lists every missing cell
by name and count while the projection shows where they go. A right-click-constructed multiblock
will not complete without room for its whole footprint, a snow layer over a cell counts as a block
rather than as air, and Electrical Progressives' Advanced Wrench fails silently on these
structures where the vanilla wrench works. The footprints are on the Cornish Boiler and Lancashire
Boiler pages.

## Is the mechanical power generator meant to sound like a ton of bells clicking?

No: the Mechanical Power Generator reuses the vanilla planetary-gear sound as a working sound,
which was a mistake, and the block is due to be replaced by a crankshaft. The Cornish engine's
clank is deliberate, the piston reaching the bottom of the cylinder. The serious half of this is
an audio leak: looping machine sounds can fill OpenAL's 250-voice limit and break all game audio,
not just this mod's. It has been found twice from different sources, so a report needs the name of
the sound that is looping rather than a video. This mod's sounds do not yet follow the game's
volume sliders. See Mechanical power and pumps.

## The recipe is invalid and I cannot craft it

The wooden axle recipe shipped with the wrong orientation for a while; it was reported, fixed,
reported again on a later version, and confirmed fixed for good on 2026-06-22. If a recipe still
looks invalid, check your version against the changelog before assuming it is new. If it is the
cost of a recipe you want to change rather than the recipe itself, see Commands and config.

## Would it work with another mod?

Nothing in this mod adapts to or guards against another mod, and there is no electricity anywhere
in the Expanded mods, so nothing interoperates with another mod's grid. The ore-mod adaptations
belong to Steelmaking Expanded. Compatibility has what each mod needs and what it patches.

## It renders for the player who built it, and the server restart eats the rest

Every one of these is state rebuilt on chunk load rather than a separate bug per symptom: a
multiblock that looks complete only to its builder until a restart, pipes that implode on restart
with their machines off, and a login pressure spike bursting pipes and boilers that have relief
valves fitted. None of it is fixed as of 2026-09-14. There is nothing to do in the moment beyond
what the boiler question above says: vent and stop a pressurised run before a restart.

## How do I change the numbers this mod uses?

`/exmod config` and `/exmod recipes` are Expanded Library's own commands, not this mod's, and the
mod id is the section name, so tuning this mod is `/exmod config ppex ...`. It edits the `ppex`
section of `ModConfig/ex_values.json` and applies live. `/exmod recipes ppex <level>` only sets the
recipe cost level and applies on the next world reload; the per-recipe numbers live in the `ppex`
section of `ModConfig/ex_recipes.json` and are edited on disk. The pipe capacity, leak and
evaporation numbers are Expanded Library's, in its `exlib` section. Every key and its default is
listed on Commands and config.
