---
title: Smoke stack
---

The [[Smoke Stack Intake]] starts this build and is the only block of it you connect a pipe to.
Why a furnace needs a stack, and where it sits in the gas network, is on [[Hot blast]].

## The shape of it

The stack is a squat three by three base with a single-cell flue running up out of it, twelve
levels in all. The Smoke Stack Intake is the control block and sits at the bottom of the front
face, at ground level, with one level of base below it.

- The base is three levels of refractory brick, three by three. The bottom level is solid; from
  the intake's level up, the centre cell of each course, one cell behind the intake, is left open
  as the bottom of the flue.
- The chimney above it is nine more levels: four brick cells around one open cell, straight up. Any
  ordinary brick does for these, fire clay, clinker or a brick course of any colour.

## Build order

1. Find a spot with twelve clear levels and a pipe route back to the furnace's exhaust main. A roof
   or an overhang inside those twelve levels blocks the build; nothing above them is checked.
2. Place the intake facing the way the pipe will arrive; it is the control block.
3. Hold ctrl and shift and right-click it for the build outline and the chat list of what is still
   missing.
4. Build the refractory base, then the chimney course by course, leaving the middle cell of each
   course open.
5. The intake reports "Smoke Stack structure is complete!" when it is done, and it starts venting
   whatever the network gives it.

## Running it

The stack is the safety valve of the whole gas side, and it is the reason a furnace keeps melting
while its stoves change over. It pulls 96 L/s off the network it is connected to and throws it
away, which is more than one blast furnace makes at full melt rate.

1. Pipe it to the same exhaust network the furnace and the stoves are on. It only draws from a run
   whose pipe faces back at the intake.
2. Put a pressure-relief valve in the branch that leads to it, so the stack takes the surplus and
   the stoves take the rest. A fresh valve gates at 1.0 atm, which is under the 2.0 atm the furnace
   pushes its exhaust to, so it opens under load.
3. Look at the intake to see what it is actually clearing: `Consuming 96.0 L of Gas` when the run
   has that much in it, less when it has not. The figure is what the stack took on the last tick.

Smoke rises from it while it is venting, sooty for exhaust and pale for steam. A stack passing
plain air shows nothing, which is not a fault.

## What goes wrong

**The furnace chokes anyway.** The stack is on a network the furnace's flue cannot reach, or the
branch to it has no open path. A furnace with nowhere to put its exhaust stops melting and says
"Exhaust network is full! Production halted."

**It vents nothing.** The intake's connector face is not looking at a pipe, or the run it is on
holds no gas.

**A stove swap puts the furnace out.** The overflow branch was built after the fact. It has to
exist before the first swap; see [[Hot blast]].

**The build will not complete near the top.** A snow layer is not air, and neither is a vine or a
torch. Clear the column.
