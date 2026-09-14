# Steelmaking Expanded: frequently asked questions

Questions that need both Steelmaking Expanded and Pipes and Power Expanded to answer. For
pipes-and-power-only questions, see the [Pipes and Power Expanded FAQ](../../ppex/FAQ/).

## It just says extinguishing

The burden only lights and stays lit when air is already flowing into the tuyeres under pressure;
lighting a burden with the blowers off is the single most common failure reported. The "10/10
liters of blast" reading on the furnace door is the burden charge, not a live air supply, and
reading it as proof the blast is connected has cost more than one player a run. The numbers the
shipped mod uses: a tuyere draws about 20 L/s at melt rate 1.0, so two draw around 40 and one in
overdrive around 80, and the furnace lights once the blast holds 1.5 atm or more. A mechanical-power
blower reaches 2 atm, which is enough for the furnace but not for the Bessemer converter's 2.5. Keep
an overflow line to the smoke stack valved at 0.5, because the furnace chokes at 0.8. The 1.5 atm
threshold dates from 2026-08-13 (it was 2.5 before); a furnace built before that date needs its
door broken and replaced to pick it up. See [Steelmaking: Blast Furnace](handbook/blastfurnace/) and
[the blast furnace build and its first tap](Instructions#the-blast-furnace-build-and-its-first-tap).

## I do not quite understand how the cowper stoves are supposed to function

The bottom intake and outlet carry furnace exhaust, the upper passthrough and outlet carry air, and
each stove has two valves. Run one at a time: exhaust first to warm the heatsinks, then air, since
both valves open at once is what "exhaust mixes with air" means. Leftover air in a segment blocks
reheating, because the stove cannot push into a side already at higher pressure. Burning coal under
the stove only speeds heating and turns the output into exhaust, so the fire must be out before
switching to blast, and an idle stove neither heats nor cools by design. As of 2026-09-10 a player
reported the stoves broken and skippable by running cold air straight into the furnace instead,
against "you absolutely need two" from June; no owner statement confirms or denies that. See
[the cowper stove](Instructions#the-cowper-stove).

## Is it compatible with Improved Metallurgy / IME / Expanded Matter?

Improved Metallurgy implements the same mechanic a different way, so pick one mod, not both: running
them together crashed a whole server through duplicate nugget-crushing patches, so "pick one" means
they collide, not that they are merely redundant. Expanded Matter's crushed ores already work; a
failure there means an out-of-date install. Interesting Mining and Extraction is unanswered; the
community's own guess is that it would need its own crusher recipe and probably is not worth adding.

## How do I pick up molds with molten ingots without getting hurt, and why do they take 70 minutes to cool?

Wear heavy leather or blacksmithing gloves; tongs do not work and were rejected by design, since the
player holds a mold with both hands. A filled mold can be picked up, unlike vanilla, but landing in
a non-active slot or swapping slots destroys the molten metal, so keep it in your active slot until
it solidifies. Cooling rides on the item stack, not a fixed timer: about three minutes from a
crucible pour against roughly seventy real minutes from a canal tap, so count your molds before
tapping rather than after. Pulling a mold off its pedestal onto the ground cools it faster. Ceramic
molds are planned to be replaced by sand and cast iron molds, so this answer is dated. See
[Steelmaking: Molten Metal Casting](handbook/casting/) and
[molds and casting](Instructions#molds-and-casting).

## How do I know when the steel is done?

The stone coffin makes blister steel and the crucible process is bolted onto the end of that route,
not a replacement for it. The coal pile under the cementation furnace burns for eight in-game hours
regardless of coal type and must be topped up. You tell it is done by looking at the coffin, and the
door is safe to open at that point. The Bessemer step needs no new recipe logic to accept a new
alloy; it would need converter, ingot, plate, toolhead and mold changes together. See
[Steelmaking: Bessemer Converter](handbook/bessemer/) and
[the Bessemer run](Instructions#the-bessemer-run).

## The molten will not travel more than four canals

A real regression once stalled metal after four canal segments; it was fixed the same day it was
reported, so check your version rather than assume it is still open. Molten metal does not
back-flow: a tap with nothing under it solidifies rather than waiting, and a junction always fills
its first non-full exit first. Canal start blocks accept a manual crucible pour as a valid target,
and chiseling one out means chiseling solidified metal, which does not work on the canal's
non-cube shapes.

## It extinguishes every time I switch the stoves

Build the overflow line to the smoke stack before the first switch: the exhaust has nowhere else to
go, and a valve set to 0.5 works because the furnace chokes at 0.8. Cycling a valve faster does not
fix this; the fix is the overflow line. Merge the output network so both tuyeres share pipes, or
temperature drops below 1500C on every switch. See
[the cowper stove](Instructions#the-cowper-stove).

## All the pressure drops the moment we start refining

The Bessemer converter's input is not a network block, so a pipe must sit between the blower and the
converter. The converter needs 2.5 atm and a mechanical-power blower cannot reach that; a
steam-driven blower is required. Building pressure above 2.5 before starting is not a complete
answer: a mid-blow sag to about 2.48 has been reported with no fix, so expect the run to need
watching, not just a high starting number. See
[the Bessemer run](Instructions#the-bessemer-run).

## How do I change the numbers this mod uses?

`/exmod config` and `/exmod recipes` are Expanded Library's own commands, not this mod's; the mod id
is the section name, so tuning smex is `/exmod config smex ...`. That command edits
`ModConfig/smex_values.json` and applies live. `/exmod recipes smex <level>` only sets the recipe
cost level, which applies on the next world reload; the per-recipe numbers themselves live in
`ModConfig/smex_recipes.json` and are edited on disk, not through either command.

## The update broke my burden, and the hopper will not take crushed coke any more

The hopper takes plain coke now, not crushed coke; the crushed-coke recipe was disabled for
Expanded Matter compatibility, and a migration for burden built before that change is still owed.
If your burden looks wrong after updating, that is the known gap, not a new bug.

## What refractory does the blast furnace need?

Tier 3 refractory brick was required through June 2026; the furnace has accepted any refractory
tier since 2026-08-28. Build with whatever refractory you have on a current install; only an older
build made before that date needs tier 3. Check: the furnace's multiblock overlay completes with
the brick you used.
