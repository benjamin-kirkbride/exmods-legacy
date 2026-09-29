using System.Collections.Generic;
using ExpandedLib.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PipesAndPowerExpanded.ClosedLine;

/// <summary>The chat line that tells a player the line is closed, sent once per player per world.
/// The players told are kept in the save's mod data under <c>ppex:closedline-notified</c>.</summary>
public static class ClosedLineNotice {
  /// <summary>The notice's lang key; its <c>{0}</c> takes the names of the enabled
  /// successors.</summary>
  public const string LangKey = "ppex:closedline-notice";

  /// <summary>The lang key that joins two names, <c>{0}</c> and <c>{1}</c>, in the notice.</summary>
  public const string AndKey = "ppex:closedline-notice-and";

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
      Text(api, player.LanguageCode),
      EnumChatType.Notification
    );
    notified.Add(player.PlayerUID);
    ExWorldData.Set(api, "ppex", NotifiedKey, notified);
    return true;
  }

  /// <summary>The notice in <paramref name="langCode"/>, naming each of
  /// <see cref="ClosedLineModSystem.Successors"/> enabled in <paramref name="api"/>'s game by its
  /// modinfo name, joined through <see cref="AndKey"/>.</summary>
  public static string Text(ICoreAPI api, string langCode) {
    string names = "";
    foreach (string id in ClosedLineModSystem.Successors)
      if (api.ModLoader.GetMod(id)?.Info.Name is { } name)
        names =
          names.Length == 0 ? name : Lang.GetL(langCode, AndKey, names, name);
    return Lang.GetL(langCode, LangKey, names);
  }
}
