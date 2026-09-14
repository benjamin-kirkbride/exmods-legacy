---
hints: [blockhelp-valve-toggle]
---

A shut-off with no numbers on it: right-click with an empty hand to toggle it, and the model's own
pose shows whether it is open or shut. Open, it is an ordinary pipe node. Closed, it severs the
network at its own cell, splitting the two sides into separate pools at separate pressures.

Opening a closed valve merges the two networks at once, and the merged run settles at their
average, which reads like the pressure has been dumped when it has only spread over more pipe. A
closed valve also does not cap the far side: if nothing is built past it, that face is an open end
for whatever network sits on the other side and leaks 8 L/s like any other hole.
