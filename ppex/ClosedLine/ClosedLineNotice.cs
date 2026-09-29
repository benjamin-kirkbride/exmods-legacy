using System.Collections.Generic;
using ExpandedLib.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>The chat line that tells a player the line is closed, sent once per player per world.
/// The players told are kept in the save's mod data under <c>ppex:closedline-notified</c>.</summary>
public static class ClosedLineNotice {
  /// <summary>The notice's lang key.</summary>
  public const string LangKey = "ppex:closedline-notice";

  private const string NotifiedKey = "closedline-notified";

  /// <summary>Sends the notice to each joining player not yet told in this world.</summary>
  public static void Register(ICoreServerAPI api) =>
    api.Event.PlayerJoin += player => Send(api, player);

  /// <summary>Sends the notice to <paramref name="player"/> in their language, and records them in
  /// the save, unless the save already records them.</summary>
  /// <returns>Whether the notice was sent.</returns>
  public static bool Send(ICoreServerAPI api, IServerPlayer player) {
    List<string> notified =
      ExWorldData.Get<List<string>?>(api, "ppex", NotifiedKey) ?? [];
    if (notified.Contains(player.PlayerUID))
      return false;

    player.SendMessage(
      GlobalConstants.GeneralChatGroup,
      Lang.GetL(player.LanguageCode, LangKey),
      EnumChatType.Notification
    );
    notified.Add(player.PlayerUID);
    ExWorldData.Set(api, "ppex", NotifiedKey, notified);
    return true;
  }
}
