---
title: Hot blast
covers:
  - "smex:cowperstove-intake*"
  - "smex:cowperstoveheatsink*"
  - "smex:smokestack-intake*"
order: 2
version: 0.9.8
---

Hot blast is the furnace's own flue gas, soaked into a brick core and given back to the air on its
way to the tuyeres. A cowper stove is that core: it charges on exhaust, then discharges into air.
Nothing about it is required to make iron, and everything about it is worth building anyway,
because it roughly triples the melt rate.

The builds are on the [[Cowper stove]] and [[Smoke stack]] pages.

## What it buys

A furnace on plain blower air tops out at a 1540 C hearth, climbs at 4 C/s and melts at 0.61x.
Blast at 1240 C lifts that to a 1740 C hearth, an 8 C/s climb and 2.0x, which is 20.4 units of iron
per second against 6.2. There is no threshold to cross: the ceiling and the climb both interpolate
from the cold figures to the hot ones across the blast temperature, so a part-charged stove is
worth part of the boost. 1240 C is where the gain stops, and it is also the hottest a stove core
gets.

## The four connections

A stove is one block tall where you stand and four gas connections in two pairs. Taking the
Cowper Stove Intake block as the front, at its own level:

| connection | where | carries |
|---|---|---|
| the intake block itself | front, low | furnace exhaust in |
| outlet | front, one block up | hot blast out |
| passthrough | back, one block up | air in from the blowers |
| outlet | back, low | spent exhaust on to the stack or the next stove |

The front pair is the furnace's side, exhaust in at the bottom and hot blast out at the top; the
back pair is the blowers' side, air in at the top and spent gas out at the bottom. Each stream
crosses the core on its way through. The four Heat Sinks stand in the column between them and show
the core temperature when you look at one. A pipe merely routed past the intake face is not plumbed
in: the stove only draws from a run whose pipe presents a connector back at it.

## Charging

While exhaust is arriving, the core takes a share of the gap between the gas temperature and its
own, every second, and the spent gas leaves the back outlet at 40 per cent of the temperature it
came in with. The stove draws 24 L/s. Furnace exhaust arrives at 80 per cent of the hearth
temperature, so about 1230 C from a furnace on cold blast.

| coal pile below the intake | share per second | time to close half the gap |
|---|---|---|
| none | 0.0012 | 9 min 40 s |
| any coal but anthracite | 0.0048 | 2 min 25 s |
| anthracite | 0.0064 | 1 min 50 s |

The core stops at 1240 C whatever it is fed. The coal pile goes in the cell directly below the
intake block, which the build leaves open for it.

## Discharging

While air is passing, the air leaves at the core's temperature and the core loses heat in
proportion to the air actually drawn against its rated 24 L/s. At a full draw it sheds half of its
margin over the incoming air in about 9 min 40 s. A stove doing nothing at all still bleeds toward
ambient with a half-life of about 19 minutes, so a charged stove cannot be banked for later.

That is why stoves come in pairs: one charges on exhaust while the other blows, and they change
places before the blowing one runs down.

## Running a pair

1. Build both stoves, with their heat sinks inside and the overflow line to the Smoke Stack
   already valved and open. Build that line before the first swap, not after it.
2. Merge the hot-blast side of the network so both tuyeres draw from the same pipes. A split
   output drops the blast temperature on every swap.
3. Run the furnace on cold blast first. A stove has nothing to charge from until the furnace has
   been alight for a while.
4. Charge stove A: open its exhaust valve, leave its air valve shut. It reads "Heating Up!" and
   the heat sinks climb.
5. Swap: shut A's exhaust valve, open A's air valve. It reads "Heating Air" and the furnace picks
   up the boost.
6. Charge B the same way while A blows, and change them over when A's heat sinks drop toward the
   point where the boost is no longer worth it. Nothing in the code fixes that point; the in-game
   handbook suggests swapping below 800 C.
7. One valve per stove at a time, always. Both open is the fault below.

## The smoke stack

The stack is the safety valve of the whole gas side. It draws 96 L/s off the run and vents it,
which is more than one furnace makes at full melt rate. Without it, or without a path to it, the
exhaust run fills, the furnace chokes and the melt stops.

Route the surplus to the stack through a pressure-relief valve teed off the exhaust main. The
furnace pushes its exhaust to 2.0 atm, and a freshly placed relief valve gates at 1.0 atm, so the
default setting opens under load. A valve at 0.5 atm, the figure the owner gave in June 2026, also
works; the advice behind it, that the furnace chokes at 0.8 atm, is older than the 0.9.5 release
that raised the furnace's exhaust ceiling to 2.0 atm.

## What goes wrong

**"Cannot heat up, exhaust mixes with the air!"** Both valves of one stove are open. Exhaust and
air are arriving at the same stove, and it will not charge. Shut the air valve: the stove vents the
gas stranded in its passthrough on the next tick and charges from the one after, so there is
nothing to clear by hand. A stove that keeps the message has its air valve still open somewhere.

**The furnace goes out every time you swap.** The exhaust has nowhere to go for the moment the
valve is shut. Build the overflow to the smoke stack, and merge the hot side so both tuyeres keep
drawing during the swap. Cycling the valves faster does not help: no blast at 1.5 atm for
30 seconds is what puts the furnace out, and a second disruption at the same time puts it out
at once.

**The blast temperature falls below 1500 C on every swap.** The output networks are still
separate, so the tuyeres lose their supply while the stoves change places.

**A cold stove reads Idle and does nothing.** With no exhaust to charge from and no air to give
heat to it has nothing to do, and that is not a fault. A charged stove is a different matter: it
bleeds toward ambient whatever it is doing, so it cannot be banked. Charge one when it is about to
blow.

**Exhaust arrives at a tuyere.** The furnace counts that as a disruption and starts its extinguish
clock. Keep the exhaust and air sides apart the whole way; a stove hands on whatever medium its
air passthrough carries.

**The coal fire.** The in-game handbook warns that burning low grades of coal makes the stove give
off exhaust from its own main output. The code in 0.9.8 does not do that: the pile only picks the
charging rate above, and a stove hands on whatever medium arrives at its passthrough. Putting the
fire out before a stove blows still costs nothing, and a stove is meant to blow after it is
charged, not while it is.

## What changed, and when

| claim | when |
|---|---|
| "you absolutely need 2 cowper stoves and a smoke stack for the system to work" (owner) | 2026-06-02 |
| "those are not required any more to smelt" (owner) | 2026-08-16. The code agrees: cold blast clears iron's melting point on its own |
| "the cowper stoves are broken at the moment, just run cold air straight into the furnace" (a player, not the owner, and unconfirmed) | 2026-09-10 |

The stove code in 0.9.8 does charge and discharge as described here. A furnace that will not take
the boost is usually one of the faults above, and the cold-blast route is always available as a
fallback: it is slower, not broken.
