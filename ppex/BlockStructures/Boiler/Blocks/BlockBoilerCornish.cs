using ExpandedLib.Industry.MechanicalPower;
using ExpandedLib.Structures;
using ExpandedLib.Registries;

namespace PipesAndPowerExpanded.BlockStructures.Boiler.Blocks;

/// <summary>
/// The Cornish boiler mega-block (iron, low-pressure entry tier). All behavior lives
/// in <see cref="BlockBoiler"/>.
/// </summary>
[BlockRegister]
public partial class BlockBoilerCornish
  : BlockBoiler,
    IFillerHost,
    IBoilerGeometry { }
