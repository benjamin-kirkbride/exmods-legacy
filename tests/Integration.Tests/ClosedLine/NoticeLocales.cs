using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Vintagestory.API.Config;

namespace Integration.Tests.ClosedLine;

/// <summary>
/// The locales <c>closedline-en</c>, <c>closedline-ru</c> and <c>closedline-uk</c>: each translates
/// the ppex keys of ppex's shipped lang file of that locale and formats its arguments into the text,
/// as the game's translation does.
/// </summary>
/// <remarks>Registered once, before any test runs, since <see cref="Lang"/>'s locales are shared by
/// every test in the process.</remarks>
internal static class NoticeLocales {
  /// <summary>The prefix before <c>en</c>, <c>ru</c> or <c>uk</c>.</summary>
  public const string Prefix = "closedline-";

  /// <summary>Adds the three locales to <see cref="Lang.AvailableLanguages"/>.</summary>
  public static void Register() {
    foreach (string locale in new[] { "en", "ru", "uk" }) {
      Dictionary<string, string> entries = JObject
        .Parse(
          File.ReadAllText(
            Path.Combine(RepoPaths.Root, "ppex", "assets", "ppex", "lang", locale + ".json")
          )
        )
        .Properties()
        .ToDictionary(p => "ppex:" + p.Name, p => (string)p.Value!);
      var service = Substitute.For<ITranslationService>();
      service.LanguageCode.Returns(Prefix + locale);
      service
        .HasTranslation(Arg.Any<string>(), Arg.Any<bool>())
        .Returns(ci => entries.ContainsKey(ci.ArgAt<string>(0)));
      service
        .Get(Arg.Any<string>(), Arg.Any<object[]>())
        .Returns(ci =>
          string.Format(
            CultureInfo.InvariantCulture,
            entries[ci.ArgAt<string>(0)],
            ci.ArgAt<object[]>(1)
          )
        );
      Lang.AvailableLanguages[Prefix + locale] = service;
    }
  }
}
