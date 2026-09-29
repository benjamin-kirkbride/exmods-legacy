# Getting started

The order below is the order to build in. Each stage runs on the one before it, and every stage
except the last is worth stopping at: a furnace on cold blast makes iron all day without stoves or
a converter.

## Before anything

Steelmaking Expanded 0.10.0 needs Expanded Library 0.8.3 or later and
[Pipes and Power Expanded](/current/ppex/Home/) 0.7.0 or later, and all three have to move
together. It also needs you to be working iron already: the furnace is built from refractory
brick, and the machines around it want plates, rods and nails. Read [[Compatibility]] first if you
run other ore or metallurgy mods, because crushing recipes are where they collide.

There is no single shopping list for the whole mod, and there does not need to be one: every build
page here carries a materials table computed from its own layout, and every block page carries its
recipe. Work through the stages below and take each list as you come to it.

## Feed

Nothing goes into the furnace as ore. Set up the supply first: a pulverizer to crush iron ore, a
coke oven for coke, and a source of lime. Charcoal works in place of coke at twice the pieces, so a
charcoal pit will keep a furnace fed while the coke ovens catch up. The rates are on
[[Ironmaking]].

## The hoppers and the furnace

Build the [[Blast furnace]] around its door, with the Reinforced Hopper and the Bell Hopper
stacked on top of the shaft. Stock the hopper and let the bell drip burden down until the
door reads 320 of 320. Do not light anything yet.

## Air

The furnace cannot be lit without blast. Build a power source and a blower: either a steam engine
from Pipes and Power Expanded driving an Air Blower, or a waterwheel through one large gear driving
a Twin-Tub Blower. Pipe it to both Tuyeres and check the door shows 1.50 atm or more before the
torch comes out. The air budget, and which blower reaches which pressure, are on [[Ironmaking]].

## Exhaust

Build the [[Smoke stack]] and pipe the furnace's two gas outlets to it before the first
light. A furnace with nowhere to put its flue gas chokes and stops melting. This is also the branch
the stoves will later share, so leave room for a valve.

## The first iron

With air flowing and the stack clearing, light the charge and shut the door. At 1482 C the furnace
starts melting. Put a molten canal start under the iron tap, run canal out to where you want the
metal, and open the tap. [[Molten metal]] covers the flow rules, the capacities and how a run
freezes.

## Casting

Put a [[Molten Canal (Mold Pedestal), brick]] (or the cobblestone kind) on the end of a run, set a
mold on it and open the pour. Wear gloves before you touch anything that has been filled.
[[Casting]] has the mold sizes, the cooling rule and the two ways a filled mold is lost.

## Slag

Tap the slag from the upper tap on the other side of the furnace, or the melt stalls when the slag
reservoir fills. Freeze it and chip it out for fertilizer, mortar and paving:
[[Slag and its products]].

## Hot blast

Now triple the output. Build a pair of [[Cowper stove]] towers, charge one on the furnace's
exhaust while the other blows preheated air into the tuyeres, and swap them over with valves. Build
the overflow line to the stack before the first swap, or the swap puts the furnace out.
[[Hot blast]] is the whole loop.

## Steel

Last, and only on a line that already runs: the Bessemer converter and its vessel. It wants
blast at 2.5 atm, which only a steam-driven blower makes, an axle for mechanical power, and a canal
in and a canal out. [[Bessemer process]] has the pressures, the timings and how much scrap a blow
can carry.

## Tuning

Every number in all of this is a config key you can change in place, live, with
`/exmod config smex`. See [[Commands and config]].
