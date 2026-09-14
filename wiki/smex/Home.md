# Steelmaking Expanded (smex)

Steelmaking Expanded adds an industrial-era iron and steel production chain on top of vanilla
metalworking. Current version is 0.9.8. It needs Expanded Library 0.7.2 and Pipes and Power
Expanded. It carries no guard against later exlib versions; the game's loader simply fails to load
this mod at all once exlib 0.8 or later is installed, since its assembly reference no longer
matches.

It adds a blast furnace, a tall multiblock of refractory brick fed by a hopper pair that combines
iron ore, lime and fuel into burden, pooling molten iron and slag once fired above iron's melting
point; hot blast machinery, cowper stoves that recycle furnace exhaust into blast air, a smoke
stack, and two ways to pressurise the blast line, a steam-driven air blower or the axle-driven
twin-tub blower; a molten canal network that plumbs liquid metal through rock-built canals,
furnace taps, a pouring canal tap, mold pedestals and molten barrels for bulk storage; casting,
including plate, quad-rod and double-ingot ceramic molds and the casting of large molds directly
under a canal tap; the Bessemer converter, a 3x3x3 vessel of tier-2 refractory brick that blows
molten iron into steel using mechanical power and a high-pressure blast line; and a slag chain
that turns solidified slag into mortar ingredient or fertilizer and scrap iron bits back into
crushed iron.

The in-game handbook ships five articles - overview, blast furnace, hot blast, casting and
Bessemer - with full build costs and operating procedures. Gameplay tunables live in
`ModConfig/smex_values.json`, with recipe and construction costs in `ModConfig/smex_recipes.json`,
both editable live with `/exmod config` and `/exmod recipes`.

smex's successor is Steel Industry Expanded, a new mod built on exlib 0.8 and later rather than an
update to this one; worlds do not carry over between the two.

See the [FAQ](FAQ) for questions players actually asked, and
[Instructions](Instructions) for the setups they most often got wrong.
