# Frequently asked questions

Questions that need both Steelmaking Expanded and Pipes and Power Expanded to answer. For
pipes-and-power-only questions, see the [Pipes and Power Expanded FAQ](/current/ppex/FAQ/).

## Why does the door say Extinguishing?

The burden only lights, and only stays lit, while air is already arriving at the tuyeres under
pressure. Lighting with the blowers off is the most common failure reported with this mod, and it
gives you ten seconds before the piles go out. The gate is 1.5 atm, and the door prints both the
rate arriving and the pressure standing; a line moving plenty of air under the gate reads fine on
the first line and puts the furnace out anyway. A boiler safety valve setting is a different
quantity from the pressure at the tuyere.

The line at the top of the door, `Burden loaded: 320 / 320`, is not the air supply. It counts the
charge in the shaft, and the two blast lines under it are the only ones that say anything about
air. Reading this line as the air supply is the easy way to call a furnace broken when it is not.

A lit furnace goes out for four reasons: burden robbed below 144, exhaust arriving at a tuyere, no
blast at 1.5 atm, or the door left open. One of them gives 30 seconds of grace, or 10 for the door;
two at once end it immediately. The whole model, with the air demand per tuyere and the blower
figures, is on [[Ironmaking]]; the build and the lighting order are on [[Blast furnace]].

The 1.5 atm gate dates from 2026-08-13, when it was lowered from 2.5. A furnace built before that
keeps the old figure until its door is broken and replaced.

## How do the cowper stoves work?

Four connections in two pairs. The intake block itself takes furnace exhaust in and the outlet
directly above it gives the preheated air out; at the back, the passthrough one level up takes air
in from the blowers and the outlet below it sends spent exhaust on to the stack. One valve open at
a time: exhaust first to charge the core, then air to blow it into the furnace. Both open is what
"exhaust mixes with the air" means, and the stove will not charge in that state.

Anthracite is not required. The coal pile under the intake only speeds charging, and the stove
charges without one, just slowly. A cold stove reading Idle is not broken either; it simply has
nothing to do. A charged one is not storage, though: it bleeds toward ambient whatever it is doing,
halving its margin over the air around it about every 19 minutes. Full detail, rates and the pair
loop are on [[Hot blast]]; the build is on [[Cowper stove]].

## Is it compatible with Improved Metallurgy / IME / Expanded Matter?

Expanded Matter and IndustrialStory are the two ore mods the code adapts to: their crushed ores
are accepted as furnace feed, and this mod stands back from vanilla nugget crushing while either
is loaded. No other ore mod has an adaptation, and crushing recipes are where ore mods collide:
two mods that both rewrite how a nugget crushes can produce a loop where iron makes more iron, so
run one such mod at a time. [[Compatibility]] lists what this mod patches and adapts to.

## How do I pick up molds with molten ingots without getting hurt, and why do they take 70 minutes to cool?

Wear heavy leather gloves or a blacksmith's gloves. Tongs do not work and were rejected by design,
since a mold is held with both hands. Metal over 200 C in a mold in your hand costs you a health
point a second without them.

A filled mold can be picked up, unlike vanilla, but liquid metal only rides in your active hand:
another hotbar slot, a bag, a chest or a mold rack empties it at once. On the cooling, a mold under
an open pour does not cool at all, because every unit that lands restamps the cast to the canal's
temperature. It starts cooling when it is full, when the run empties, or when you shut the pour,
and taking it off the pedestal takes it out of the pour. A canal-fed cast can run to about 70 real
minutes against about 3 for a crucible pour. Count your molds before you tap. See [[Casting]].

## How do I know when the steel is done?

The converter's own panel says so: "Steel ready! Pour it out." Until then it counts "Refining..."
in per cent, and the blow takes 10 minutes at the 2.5 atm gate, down to 2 minutes 30 at 6 atm.
Then sprint and right-click the control, held for a second, to pour it into the output canal.

The Bessemer route is not the stone coffin route. Blister steel from a coffin still works as it
always did, and the coal pile under a cementation furnace still burns for eight in-game hours
whatever coal it is. The converter is a different, parallel way to make steel, and it is covered on
[[Bessemer process]].

## Why will molten metal not travel more than four canals?

There is no distance limit in the code. A run reaches as far as you feed it, and what stops it is
metal freezing in a cell: below its melting point a cell sets solid, drops off the network, and
blocks everything past it. A four-block stall was a real regression in earlier versions, and 0.9.7
fixed it by doubling canal capacity and throughput and dropping the head the old flow rule cost at
every block. Check your version against 0.9.7 before assuming it is that.

Metal also does not back up and wait: a tap with nothing under it pours into a start that fills and
freezes, and a junction fills the first exit that is not already full. [[Molten metal]] has the flow
rules, the capacities and the freezing thresholds.

## Why does it extinguish every time I switch the stoves?

The exhaust has nowhere to go for the moment a stove's exhaust valve is shut, so build the overflow
branch to the Smoke Stack Intake before the first swap. A pressure-relief valve in that branch does
it; a fresh valve gates at 1.0 atm, which is under the 2.0 atm the furnace pushes its exhaust to.
A valve set as low as 0.5 atm also works, though the 0.8 atm choke point it was originally sized
against predates the 0.9.5 release that raised the furnace's exhaust ceiling.

Cycling the valves faster does not help, and neither does a separate pipe run per stove: merge the
hot side so both tuyeres keep drawing through the swap, or the blast temperature drops on every
change. See [[Hot blast]].

## Why does the pressure drop the moment we start refining?

The converter's gas intake is a port, not a pipe: a pipe has to sit in the cell in front of it,
facing back at it. It needs 2.5 atm, which a Twin-Tub Blower cannot reach at all, so the blast has
to come from a steam-driven Air Blower with enough steam behind it.

Starting above 2.5 atm is not the whole answer. The converter's draw climbs with the blow rate, up
to 48 L/s at 6 atm, and a sag to about 2.48 atm partway through a blow can appear, with no fix
recorded. Build the supply for the rate you mean to run and watch the reading
through the run. [[Bessemer process]] has the pressure table.

## How do I change the numbers this mod uses?

`/exmod config` and `/exmod recipes` are Expanded Library's commands, not this mod's, and the mod id
is the section name: `/exmod config smex <key> <value>`. That writes `ModConfig/smex_values.json`
and applies live. `/exmod recipes smex <level>` only picks a cost profile, and it lands on the next
world reload; the per-recipe numbers live in `ModConfig/smex_recipes.json` and are edited there.
Every key and its default is listed on [[Commands and config]].

## Why did the update break my burden, and why will the hopper not take crushed coke any more?

The hopper takes whole coke now, two per batch, or four charcoal. Crushed coke was retired in 0.9.5:
its crafting route is gone and it is hidden from creative and the in-game handbook, though existing
stacks are migrated as chunks load. If the furnace refuses your fuel after an update, that is the
change, not a bug.

More broadly, the 0.9.x updates changed a lot of block entities, and the migration system is not a
guarantee. A block that looks wrong after an update has to be broken and replaced.

## What refractory does the blast furnace need?

Any tier, and they can be mixed. Tier 3 was the only accepted brick through June 2026 and any
refractory has worked since late August 2026. There is one exception in the mod: the Bessemer
Converter vessel's construction stages want tier-2 refractory brick specifically, and nothing else
will do.

## Why does it render only for the player who built it, and why does a server restart eat the rest?

Two different things, with two different answers.

The Reinforced Hopper losing or duplicating items unless they were shift-clicked in was a packet
bug that a later fix closed. On a current install the hopper syncs its contents to everyone.

The rest is open. Multiblocks that render as water for every player except the builder until a
restart, pipes that implode on a restart with the machines off, and a pressure spike on login have
all been reported and none has a recorded fix. They are one failure, not three: machine state is
rebuilt when a chunk loads. Until that changes, stop the machines and drain what you can before a
planned restart, rather than leaving a plant pressurised and full of metal.

## Why will the multiblock not complete, and what is missing?

Hold ctrl and shift and right-click the control block: that raises the build outline and prints
every block still missing, by name and count, in chat. Red cells in the outline hold the wrong
block. The outline only appears while the structure is incomplete, and it goes away by itself when
it is done.
