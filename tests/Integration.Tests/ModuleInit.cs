using System.Runtime.CompilerServices;
using ExpandedLib.Testing;
using Integration.Tests.ClosedLine;
using Legacy.Tests;

namespace Integration.Tests;

/// <summary>
/// Registers the Vintage Story assembly resolver and the headless Lang before any test type (which
/// references the game assemblies plus exlib/ppex/smex) is touched by the runner's discovery, and
/// the closed line's notice locales (<see cref="NoticeLocales"/>).
/// </summary>
internal static class ModuleInit {
  [ModuleInitializer]
  internal static void Init() {
    VsAssemblyResolver.Register();
    TestLang.Init();
    NoticeLocales.Register();
    LegacyReleasedHistory.Register();
  }
}
