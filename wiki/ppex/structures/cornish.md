## The site

Clear a box 3 wide, 3 high and 8 long before you place anything: 2 cells at the near end for the
firebox and its door, 4 for the vessel, and 2 more at the far end for the exhaust and the back wall.
The floor of the firebox sits one level below the boiler block, so the box starts one cell below it
and reaches one cell above.

Snow counts. A snow layer over any cell of the footprint reads as a block, not as air, and will
hold the structure incomplete with nothing obviously wrong.

## Orientation

Place the boiler block standing where the firebox is to go. The vessel rises away from you and the
firebox ends up behind it, on your side, with the hatch door at the far end of those two cells. Get
this wrong and the whole thing has to come down: there is no rotate.

Hold ctrl and shift and right-click the block at any point to outline what is missing. The
projection shows the empty cells and the chat lists them by name and count, so "still needed: 6 x
Fireclay bricks" is a shopping list, not a riddle.

## Build order

1. **Lay the water line first.** It runs under the firebox floor, where nothing can reach it later:
   a Pipe Passthrough (Bend) in fireclay, turned up, directly under the boiler block, and straight
   fireclay passthroughs running from it out under the coal slot and the door. Carry the pipe on
   from there to wherever the water comes from.
2. **Wall the firebox.** Fireclay bricks around those two cells, floor, sides and roof, with the
   hatch door in the middle of the end wall at the boiler's own level. Leave the cell between the
   door and the boiler empty: that is where the coal pile goes.
3. **Set the exhaust.** A fireclay Pipe Outlet facing up, at the far end in the vessel's upper row,
   with the cell directly below it left as air. Stand an ordinary chimney on it, or run a pipe from
   it to a smoke stack.
4. **Brick the plinth and the back wall.** The vessel rests on a course of fireclay bricks, and the
   far end is closed with them.
5. **Raise the vessel.** Right-click the boiler block with each stage's materials in your hotbar.
   The base appears when the block is placed; three stages follow.

   | stage | materials |
   |---|---|
   | base extension | 6 iron or steel plates, 4 nails and strips, 8 fireclay bricks |
   | flues | 8 iron or steel plates, 4 iron or steel rods, 4 nails and strips |
   | casing | 8 iron or steel plates, 8 nails and strips, 4 iron or steel rods, 36 fireclay bricks |

   The fireclay bricks here are the item, not the wall blocks of the firebox.

6. **Take the steam off the top.** The steam connector is the cell on top of the vessel two along
   from the boiler block; the pipe goes in the cell directly above it. Everything else on top is
   solid.

The reserved cells around the vessel are not blocks you place. The construction claims them itself
and they hold the collision of the finished machine.

## The three sides

**Feedwater** enters underneath, through that bend passthrough. The boiler draws from it on its own,
up to 10 L/s, and stops at half its capacity so there is always room for steam.

**Steam** leaves the top connector, and nothing else. Until a pipe sits above that cell the neck is
open and the boiler simply bleeds steam into the air.

**Exhaust** leaves the far end. It must be able to go somewhere: a chimney on the outlet draws
exactly what the fire makes, and an open pipe end does not.

## First firing

1. Open the lid: hold right-click on the top of the vessel, one cell along from the boiler block.
   It is a hold, not a click.
2. Pour in water, one container at a time. A stack of buckets loses everything but one bucketful.
   The boiler needs 150 L before it will do anything.
3. Close the lid.
4. Lay a coal pile in the firebox slot behind the door and light it.
5. Wait. The look-at line counts up through "Heating up", and the first steam comes about three
   minutes after lighting.
6. Watch the outlet pressure climb on the steam line and not on the boiler. The vessel and the run
   equalise, so a long line takes longer to come up.

## When it goes wrong

**It says the structure is incomplete and everything looks built.** Show the outline. The usual
culprits are the cell under the outlet, which has to be air, a snow layer, and a passthrough turned
the wrong way.

**Nothing happens after lighting the coal.** Either the water is below 150 L or the exhaust has
nowhere to go. A choked boiler says so on the look-at line and snuffs its own fuel pile after 10
seconds.

**The wrench will not finish the build.** Electrical Progressives' Advanced Wrench fails silently
on these multiblocks. Use the vanilla one.

**Steam but no pressure.** An open end somewhere on the steam run bleeds 8 L/s. Turn on the network
highlight and walk the line.

**It exploded.** See Boilers: a Cornish sitting at 5 atm, still firing, with the lid shut, for
30 seconds is the one case that does it.
