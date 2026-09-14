# Pipes and Power Expanded (ppex)

Pipes and Power Expanded adds pipe networks and modular steam power machinery. It is the
infrastructure layer of the Expanded mod family and a hard dependency of Steelmaking Expanded.
Current version is 0.6.9. It needs Expanded Library 0.7.2 and refuses to start with exlib 0.8 or
later installed.

It adds iron and steel pipe networks carrying a gas or water, with pressure limits, leaking open
ends and a shared network temperature; fittings such as hand valves, directional pressure valves,
brick passthroughs and outlets, fluid intakes and a steam condenser; the compact Cornish boiler
and the heavy Lancashire boiler, both raised through right-click construction over a fire-brick
firebox; the low-pressure Watt steam engine and the high-pressure Cornish steam engine, each
driving one attached sub-machine (an MP generator for constant-power axle drive, a fluid pump for
boiler feed, or the air blower from Steelmaking Expanded); and two pumps that need no engine, the
hand-cranked manual fluid pump and the axle-driven mechanical fluid pump, both useful for filling
a boiler whose fire is out.

The in-game handbook covers build costs, operating steps and failure modes for every piece of
this machinery under its "Steam Power" articles. Gameplay tunables live in
`ModConfig/ppex_values.json`, with recipe and construction costs in `ModConfig/ppex_recipes.json`,
both editable live with `/exmod config` and `/exmod recipes`.

ppex's successor is Iron Industry Expanded, a new mod built on exlib 0.8 and later rather than an
update to this one; worlds do not carry over between the two.

See the [FAQ](FAQ.md) for questions players actually asked, and
[Instructions](Instructions.md) for the setups they most often got wrong.
