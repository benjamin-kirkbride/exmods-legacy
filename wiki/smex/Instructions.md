# Steelmaking Expanded: instructions

The setups players most often got wrong, as numbered steps with the check that tells you a step
worked. These need Pipes and Power Expanded for their blast, so read
[the boiler, engine and pipe line](../../ppex/Instructions/#the-boiler-engine-and-pipe-line) first.

## The blast furnace build and its first tap

1. Build the furnace with any refractory brick; tier 3 was required as of June 2026, but any
   refractory has worked since 2026-08-28. Check: the furnace's multiblock overlay completes with
   the brick you used.

   ::schematic{code="smex:blastfurnacedoor" views="plan,iso"}

2. Load the hopper pair with iron ore, lime and fuel, and drop the burden with ctrl plus right-click;
   a plain right-click does not drop it. Check: the burden count on the furnace door rises.

3. Turn the blowers on before lighting the burden, not after. Lighting with no air flowing is the
   single most common failure: the fire needs oxygen to spread through the charge. Check: the
   tuyeres show blast pressure moving before you light, not the burden charge shown on the door.

4. Meet the air budget: a tuyere draws about 20 L/s at melt rate 1.0, so two tuyeres draw around 40
   and one pushed into overdrive around 80; size your blowers or a throttled Cornish engine to that.
   A mechanical-power blower reaches up to 2 atm, which is enough for the furnace's own blast
   threshold, though not for the Bessemer converter's higher one. Check: blast pressure holds at or
   above 1.5 atm once the burden is lit; that threshold was lowered mod-wide on 2026-08-13, and a
   furnace built before that date needs its door broken and rebuilt to pick it up.

5. Put a mold or a canal start block under each tap before you tap. Molten metal does not back-flow,
   so a tap with nothing under it solidifies, and a junction always fills its first non-full exit.
   Check: the mold or canal fills rather than a solidified plug appearing at the tap.

6. Let the furnace manage its own iron pool; you cannot overfill it. Once molten iron or slag hits
   the furnace's own cap, the melt stalls and its status line reads a full reservoir, and tapping
   either vessel resumes it. Check: a stalled melt clears once you tap.

7. Know the timers: about 15 to 20 minutes of grace before melting starts, 10 seconds of grace on an
   interruption, 30 seconds on an insufficient air mix. If blast is lost mid-melt, the melt resumes
   with the same molten amount once blast returns. Check: the furnace's status line names the timer
   it is currently running, not a cold or extinguished state.

## The cowper stove

1. Build two stoves with heatsinks inside; a stove built without heatsinks has to be rebuilt. Wire
   the bottom intake and outlet to furnace exhaust and the upper passthrough and outlet to air.
   Check: each stove shows two distinct pipe roles, exhaust below and air above.

   ::schematic{code="smex:cowperstove" views="plan,iso"}

2. Run one valve at a time: exhaust first to warm the heatsinks, then close it and open the air
   valve. Both valves open together is what "exhaust mixes with air" means, and it is not a working
   state. Check: the stove's temperature climbs while exhaust runs and holds while air runs.

3. Clear leftover air from a segment before reheating it; the stove cannot push into a side already
   at higher pressure, so a segment that will not reheat usually still holds air from the last cycle.
   Check: the segment's pressure reads near zero before you switch it back to exhaust.

4. Build the overflow line to the smoke stack before your first switch, valved at 0.5 because the
   furnace chokes at 0.8. Check: switching a stove does not extinguish the furnace.

5. Merge the output network so both tuyeres draw from shared pipes; a split network drops temperature
   below 1500C on every switch. Check: temperature stays above 1500C across a stove switch.

6. Put the coal fire out before switching a stove to blast; burning coal turns the stove's output
   into exhaust, and an idle stove neither heats nor cools by design, which is not a fault. Check:
   the stove's air output reads as air, not exhaust, once you switch.

As of 2026-09-10 a player reported the stoves broken and skippable, running cold air straight into
the furnace instead; this is unconfirmed by the owner. Try the stove line above first.

## The Bessemer run

1. Put a pipe between the blower and the converter; the converter's input is not a network block on
   its own. Check: the converter shows a pressure reading rather than staying at zero once the pipe
   is connected.

2. Drive the blast with a steam-driven blower, not a mechanical-power one: the converter needs 2.5
   atm and a mechanical-power blower tops out at 2 atm. Check: pressure at the converter reads 2.5
   atm or above before you start the blow.

3. Expect a sag during the blow, not just at the start: pressure has been reported dropping to about
   2.48 atm partway through with no recovery, and no fix is recorded. Check: watch the pressure
   reading through the whole blow, not only before you start it.

4. Build from one side deliberately: the converter tilts to one side by design, and starting
   construction from the other side flips the whole block; no mirrored variant exists. Check: the
   tilt matches the side you meant to build from before you finish the structure.

5. Size the run to your furnace: two converters against one furnace is a mid-sized setup, and the
   only throughput figures published are from June 2026, so treat them as approximate.

## Molds and casting

1. Wear heavy leather or blacksmithing gloves before handling a filled mold; tongs do not work and
   were rejected by design. Check: no burn damage when you pick up a filled mold with gloves on.

2. Keep a filled mold in your active hotbar slot until it solidifies. Unlike vanilla, a filled mold
   can be carried, but landing in a non-active slot or being swapped out destroys the molten metal.
   Check: the mold stays in the slot you are actively holding until it cools.

3. Count your molds before tapping, not after: cooling rides on the item stack, roughly three minutes
   from a crucible pour against about seventy real minutes from a canal tap, so a canal-fed run needs
   far more molds staged than a crucible-fed one. Pulling a mold off its pedestal onto the ground
   cools it faster if you need it sooner. Check: you have enough staged molds for the tap you are
   about to open.

4. Pour a canal start block by hand from a crucible when you need to; it is a valid manual pour
   target. Chiseling a start block chisels out solidified metal, and this does not work on the
   canal's non-cube shapes further down the line. Check: the start block accepts the pour and shows
   metal inside it.
