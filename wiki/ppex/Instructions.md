# Pipes and Power Expanded: instructions

The setups players most often got wrong, as numbered steps with the check that tells you a step
worked.

## The boiler, engine and pipe line

1. Pick a footprint and clear it before you start placing: a Cornish boiler needs 3x2x4, a
   Lancashire boiler 3x2x6. Check: the placement overlay shows no missing cell as you place the last
   block; a snow layer over any cell of the footprint counts as blocked, not as air.

   ::schematic{code="ppex:boilercornish" views="plan,iso"}
   ::schematic{code="ppex:boilerlancashire" views="plan,iso"}

2. Build the boiler on its fire-brick base, run a bend passthrough directly under the boiler itself
   and a straight passthrough under each of the coalpile and the cokeoven door, and put the outlet
   on the far side with its pipe running up. Check: the boiler completes and its multiblock overlay
   switches from framed to solid.

3. Connect an engine downstream of the boiler. An engine's input face is always at the back, because
   the machine always faces one way; there is no rotate option. Check: the engine accepts the pipe
   connection only on that face.

4. Attach one sub-machine per engine: an MP generator for constant axle drive, a fluid pump for
   boiler feed, or an air blower. One engine drives exactly one sub-machine; a setup that needs both
   a pump and a blower needs a second boiler and engine. Check: the sub-machine's own indicator
   (speed, flow or blast pressure) moves once the engine is running.

5. Fill the boiler by pump rather than by hand where you can: pouring a stack of buckets loses water,
   about 40 of 50 liters from a stack of five, where filling one bucket per slot or using a pump does
   not. Check: the boiler's water level matches what you poured in.

6. Vent the exhaust before you need to. A boiler makes 16 L/s of exhaust and an open pipe only
   carries 8 L/s, so route a chimney block or a line to a smoke stack; the Cornish hatch itself opens
   on hold right-click, not a single click, and dumps pressure fast in an emergency. Check: pressure
   holds below the safety valve's setting during normal running, not just after you vent by hand.

7. Set the pressure valve the right way round: the copper-trimmed side is the input. Opening any
   valve equalizes pressure across the whole run it joins, including into the tuyeres if one is
   downstream, and a freshly placed valve does not seal until it has been opened and closed once.
   Check: the valve's gauge reads the same on both sides only when you intend it to.

8. Before a chunk reload, a relog or a server restart, vent a pressurised boiler and stop its run.
   Boilers have been found at critical pressure immediately after a reload with a safety valve
   fitted and the hatch open, and no fix for that case exists; venting first is the only known
   mitigation. Check: pressure reads near zero before you log off or the server restarts.
