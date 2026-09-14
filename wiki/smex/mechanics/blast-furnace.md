---
title: Ironmaking
covers:
  - "smex:blastfurnacedoor*"
  - "smex:hopperreinforced"
  - "smex:hopperbell"
  - "smex:blastfurnace-tuyere*"
  - "smex:blastfurnacetap*"
  - "smex:burden"
  - "smex:solidifiediron"
  - "smex:engineairblower*"
  - "smex:mpblower*"
order: 1
version: 0.9.8
---

The blast furnace smelts iron ore into a reservoir of molten iron and molten slag. It burns
burden, a packed charge of ore, fuel and flux that only its own hoppers make, and it needs air
under pressure at its two tuyeres from the moment the charge catches. Almost everything that goes
wrong with a furnace follows from the second half of that sentence.

The build is on the [[Blast furnace]] page.

## The charge

The Reinforced Hopper on top holds the feed: four iron slots, two fuel slots, two flux slots. The
Bell Hopper under it combines them into burden, buffers up to 48 units, and drops 4 units per
second down the shaft while dropping is on. One batch costs 144 ore units, 8 carbon units and
1 lime, and yields 16 burden.

| iron feed | ore units each | pieces per batch |
|---|---|---|
| crushed iron ore | 12 | 12 |
| iron nugget: limonite, hematite, magnetite | 12 | 12 |
| roasted iron ore, from mods that roast it | 14 | 10, plus 4 ore units from the crushed or nugget feed |

| fuel | carbon units each | pieces per batch |
|---|---|---|
| coke | 4 | 2 |
| charcoal | 2 | 4 |

Crushed ore and nuggets mix freely inside one batch, and so do coke and charcoal. The hopper
spends the most prepared feed first: roasted ore, then crushed, then nuggets, and coke before
charcoal. A piece cannot be split, so whatever the last one over-spends is banked against the next
batch and a long run of mixed feed pays exactly the rates above. Roasted ore is the case that
shows it: ten pieces cover 140 of the 144 ore units a batch costs, and the last 4 units come off a
crushed piece or a nugget, the rest of which is banked.

Sixteen burden melt down to 102 units of molten iron and 17 units of slag. A batch costs 144 ore
units, so one ore unit is worth 0.71 units of iron: 8.5 per piece of crushed ore or per nugget, and
9.9 per roasted piece, which carries 14 units instead of 12. Twenty vanilla nuggets smelt to one
ingot, which makes a nugget worth 5 units on the bloomery route, so the furnace is worth 1.7 times
a bloomery, or 2.0 fed roasted ore. A cast ingot is 100 units.

Burden is a coal pile item. It stacks to 128, can be laid back on the ground and picked up again,
and burns as ordinary fuel at 600 C if you ever need it to. A pile lit outside a working furnace
burns for 300 seconds and then goes cold, leaving the burden unchanged.

## The blast

The furnace draws air through its two Tuyeres. Each one asks for 20 L/s at a melt rate of 1.0,
scaled by how hot the hearth is: a quarter of it the moment the charge catches, rising with the
hearth to the full figure at iron's melting point, and on up with the melt rate after that.

| hearth | air per tuyere | both tuyeres |
|---|---|---|
| 900 C, just lit | 5 L/s | 10 L/s |
| 1200 C | 12.7 L/s | 25 L/s |
| 1482 C, iron melts | 20 L/s | 40 L/s |
| 1540 C, cold-blast ceiling | 20 L/s | 40 L/s |
| 1740 C, hot-blast ceiling | 40 L/s | 80 L/s |

Volume is only half of it. Air counts as blast only while the pipe at a tuyere carries the Air
medium at 1.5 atm or more; below that the furnace treats it as no blast at all and starts its
extinguish clock. The panel prints both figures, "Blast input: 24.0 L/s of 40.0 L/s needed" and
"Blast pressure: 1.20 atm (min 1.50 atm)", and they fail independently: a line moving plenty of
air under the gate reads fine on the first line and puts the furnace out anyway.

Two machines make air. Pressures are absolute atmospheres; a pipe run open to the air sits at
1 atm.

| blower | driven by | air | pressure |
|---|---|---|---|
| Air Blower | Cornish engine, throttle low / normal / high | 60 / 120 / 240 L/s | engine inlet steam x 0.75 |
| Air Blower | Watt engine | 90 L/s | engine inlet steam x 0.75 |
| Twin-Tub Blower | any axle, speed s | 110 x s / (s + 1.3) L/s | 2.0 atm ceiling |

The steam blower's pressure is three quarters of the steam pressure at the engine's inlet, so
2.0 atm of steam is the minimum that clears the furnace's 1.5 atm gate, and 3.4 atm of steam is
what the Bessemer process needs downstream. A boiler safety valve setting is not the blast
pressure; the figure that matters is the one the furnace door prints.

The Twin-Tub Blower flattens out as it is driven harder, because the tubs have less time to
refill: a waterwheel turning it alone settles near speed 0.29 and 20 L/s, one tuyere's worth,
while the same wheel through one large gear reaches speed 1.6 and 61 L/s, which covers both
tuyeres. It seals against 2.0 atm at most, over the furnace's gate and under the converter's, so
mechanical power makes iron but never steel.

## Heat and melt rate

The charge catches at 900 C and the hearth climbs from there. Hot blast, which the cowper stoves
make out of the furnace's own exhaust, lifts both the ceiling and the climb.

| blast temperature | hearth ceiling | climb | melt rate at the ceiling |
|---|---|---|---|
| ambient, straight off a blower | 1540 C | 4 C/s | 0.61x |
| 620 C, a half-charged stove | 1640 C | 6 C/s | 1.31x |
| 1240 C, a fully charged stove | 1740 C | 8 C/s | 2.0x |

With no blast at all the hearth still creeps up at 2 C/s on natural draught, which is not enough
to hold a furnace lit. Cold blast covers the 582 C from ignition to iron's melting point in about
two and a half minutes; a fully charged stove halves that.

Melting starts at 1482 C. The rate is the heat margin's rate multiplied by the share of the air
the hearth asked for that actually arrived, so half the air is half the melt. The margin's own
rate is 0.2 at the melting point plus 0.7 per 100 C above it, floored at 0.5 and capped at 2.0.
One melt cycle takes 10 seconds divided by that rate and turns 16 burden into 102 units of iron
and 17 of slag.

| running on | melt rate | iron | burden |
|---|---|---|---|
| cold blast at 1540 C | 0.61x | 6.2 units/s | 1.0 per second |
| hot blast at 1740 C | 2.0x | 20.4 units/s | 3.2 per second |

At full tilt the Bell Hopper's 4 units per second keeps up, but only while the Reinforced Hopper
above it has feed.

## Tapping

The hearth holds 4800 units of iron and 1200 of slag. When either fills, melting stalls until it
is drained: the panel says "Reservoir full - tap it to resume melting." and nothing is lost.

The furnace has two Molten Metal Tap blocks. The lower one on one side drains iron, the upper one
on the other side drains slag, each at 40 units per second. A tap only opens when a molten canal
start block sits below its spout, one block out from the face it points at; without one it
refuses with "No canal start found below the tap!". Toggle a tap with an empty hand. Where the
metal goes from there is [[Molten metal]].

## Exhaust

While the hearth is alight the furnace vents through the two gas outlets at the top of the shaft:
24 L/s on natural draught, or one litre of flue gas per litre of blast drawn once the blowers are
running, whichever is larger, shared between the outlets. The gas leaves at 80 per cent of the
hearth temperature and is pushed into the run at up to 2.0 atm.

If both outlets are piped and the run refuses the gas, the furnace is choked: melting halts, the
hearth falls back to its cold-blast ceiling, and the panel says "Exhaust network is full!
Production halted." A choke is a stall, not a fire out. One outlet left bare is not a choke while
the other is taking the gas; a furnace with neither outlet piped chokes as soon as the two bare
outlets have filled, which takes a couple of seconds.

## Running a heat

1. Build the furnace and check the door reports the structure complete. See
   [[Blast furnace]].
2. Stock the Reinforced Hopper with iron, fuel and lime. The Bell Hopper starts dropping on its
   own; ctrl and right-click on the Reinforced Hopper stops and restarts it.
3. Wait for the shaft to fill. The door reads `Burden loaded: 320 / 320` when there is enough to
   fire, and the hopper stops on its own once the hearth is full.
4. Start the blowers and confirm the tuyeres hold 1.5 atm or more before you reach for a torch.
5. Light the burden through the open door and close the door. Every pile in the hearth has to be
   burning: the door says "Piles are partially lit. Waiting for the fire to spread..." until they
   are.
6. Watch the state line go Firing, then Melting at 1482 C.
7. Put a canal start under each tap you mean to open, then open the taps.

## What goes wrong

**"Extinguishing in 27s..."** The furnace counts four disruptions: less than 144 burden in the
hearth, exhaust arriving at a tuyere instead of air, no blast at 1.5 atm or more, and the door
open. One disruption gives 30 seconds of grace, or 10 if it is the open door. Two at once put the
fire out immediately. The line names the count-down, not the cause; read the blast lines above it.

**Lighting first and blowing afterwards.** A hearth with a pile burning and no blast arriving
gives 10 seconds and then goes out. The burden itself is not spoiled, so relight it once the
blowers are actually holding pressure.

**Reading the burden line as the air line.** `Burden loaded: 10 / 320` is the charge in the
shaft. The blast is the two lines below it, and they appear only once something is burning.

**The fire goes out on a stove swap.** That is the no-blast disruption, and it is covered on
[[Hot blast]]: build the overflow to the smoke stack first and merge the output side so both
tuyeres draw from the same pipes.

**Melting stopped but the furnace is still lit.** Either the reservoir is full, which tapping
clears, or the exhaust is choked, which the smoke stack clears. Neither loses the burn.

**Molten iron turned into blocks in the hearth.** That is what an extinguish does: the iron
solidifies into two Solidified Iron blocks in the hearth, holding one iron bit per 5 units of
metal between them, and the slag is lost. Mine them back out.

**The hopper will not take crushed coke.** It takes whole coke now, two per batch. Crushed coke
was retired in 0.9.5 and is hidden from the in-game handbook and creative; existing stacks
are migrated as chunks load.

## What changed, and when

| change | when |
|---|---|
| blast pressure to fire lowered from 2.5 atm to 1.5 atm | 2026-08-13. A furnace built before it needs its door broken and replaced to pick it up |
| any refractory tier accepted, instead of tier 3 only | by 2026-08-28 |
| coke replaces crushed coke in the burden | 0.9.5 |
| melting became a metered rate rather than a gate, and the air, exhaust and reservoir figures moved with it | 0.9.7. Numbers quoted before that release, including the 12 L/s per tuyere and the 15 to 20 minute wait for the first melt that circulated in June 2026, no longer hold |
