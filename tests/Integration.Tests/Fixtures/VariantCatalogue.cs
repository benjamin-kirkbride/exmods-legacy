using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExpandedLib.Testing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Every block and item code registered by the game version under test and the three published
/// mods, expanded from their JSON definitions: the game's own under the install's <c>assets</c>
/// folder, the mods' from the repository. Variant groups load their states and
/// <c>loadFromProperties</c> lists; a definition's <c>allowedVariants</c> and <c>skipVariants</c>
/// apply. Loaded once per test run.
/// </summary>
internal sealed class VariantCatalogue {
  private static readonly Lazy<VariantCatalogue> Shared = new(() =>
    new VariantCatalogue()
  );

  private readonly List<Definition> _definitions = [];
  private readonly Dictionary<string, string[]> _roots = new();

  /// <summary>The catalogue of this test run's game version. Fails the calling test when the
  /// install or any definition file cannot be read.</summary>
  public static VariantCatalogue Instance => Shared.Value;

  private VariantCatalogue() {
    string? install = VsAssemblyResolver.InstallPath;
    Assert.True(install != null, "no game install found for this test run");
    string assets = Path.Combine(install!, "assets");
    string repo = ShippedJsonAssetTests.RepoRoot();
    _roots["game"] =
    [
      Path.Combine(assets, "game"),
      Path.Combine(assets, "survival"),
      Path.Combine(assets, "creative"),
    ];
    foreach (string mod in new[] { "exlib", "ppex", "smex" })
      _roots[mod] = [Path.Combine(repo, mod, "assets", mod)];

    foreach ((string domain, string[] dirs) in _roots)
      foreach (string dir in dirs)
        foreach (
          (string kind, string sub) in new[] {
            ("block", "blocktypes"),
            ("item", "itemtypes"),
          }
        ) {
          string folder = Path.Combine(dir, sub);
          if (!Directory.Exists(folder))
            continue;
          foreach (
            string file in Directory.EnumerateFiles(
              folder,
              "*.json",
              SearchOption.AllDirectories
            )
          )
            foreach (JObject def in Definitions(file))
              _definitions.Add(new Definition(domain, kind, def, this));
        }
  }

  /// <summary>
  /// What the single <c>*</c> of <paramref name="code"/> covers across every registered code it
  /// matches: the names of the variant groups it spans, plus <c>code:&lt;definition&gt;</c> for each
  /// definition whose own code it reaches into. <paramref name="allowed"/> and
  /// <paramref name="skipped"/> filter the matched part as an ingredient's lists do. Null when the
  /// code matches nothing. A code without a domain is in <c>game</c>.
  /// </summary>
  public IReadOnlyList<string>? Spans(
    string kind,
    string code,
    string[]? allowed = null,
    string[]? skipped = null
  ) {
    (string domain, string path) = Split(code);
    int star = path.IndexOf('*');
    string prefix = path[..star];
    var rx = new Regex(
      "^" + string.Join("(.*)", path.Split('*').Select(Regex.Escape)) + "$"
    );
    var spans = new SortedSet<string>(StringComparer.Ordinal);
    bool matched = false;
    foreach (Definition d in Candidates(kind, domain, prefix))
      foreach ((string full, string[] values) in d.Variants) {
        Match m = rx.Match(full);
        if (!m.Success)
          continue;
        string part = m.Groups[1].Value;
        if (allowed != null && !allowed.Contains(part))
          continue;
        if (skipped != null && skipped.Contains(part))
          continue;
        matched = true;
        int from = m.Groups[1].Index,
          to = from + part.Length;
        if (from < d.Code.Length)
          spans.Add("code:" + d.Code);
        int at = d.Code.Length;
        for (int g = 0; g < d.Groups.Count; g++) {
          int start = at + 1,
            end = start + values[g].Length;
          if (start < to && end > from)
            spans.Add(d.Groups[g].Name);
          at = end;
        }
      }
    return matched ? spans.ToList() : null;
  }

  private IEnumerable<Definition> Candidates(
    string kind,
    string domain,
    string prefix
  ) =>
    _definitions.Where(d =>
      d.Kind == kind
      && d.Domain == domain
      && ((d.Code + "-").StartsWith(prefix, StringComparison.Ordinal)
        || prefix.StartsWith(d.Code + "-", StringComparison.Ordinal))
    );

  private static (string domain, string path) Split(string code) {
    int colon = code.IndexOf(':');
    return colon < 0 ? ("game", code) : (code[..colon], code[(colon + 1)..]);
  }

  private string[] Property(string domain, string source) {
    (string dom, string path) = source.Contains(':')
      ? Split(source)
      : (domain, source);
    foreach (string d in new[] { dom, "game" }.Distinct())
      foreach (string root in _roots.GetValueOrDefault(d, []))
        if (
          File.Exists(Path.Combine(root, "worldproperties", path + ".json"))
        ) {
          JToken props = Parse(
            Path.Combine(root, "worldproperties", path + ".json")
          );
          return
          [
            .. Get(props, "variants")!
              .Select(v => (string)Get(v, "code")!),
          ];
        }
    Assert.Fail($"worldproperties {source} not found for domain {domain}");
    return [];
  }

  private static IEnumerable<JObject> Definitions(string file) {
    JToken root = Parse(file);
    IEnumerable<JToken> items = root is JArray a ? a : new[] { root };
    foreach (JToken t in items)
      if (
        t is JObject o
        && Get(o, "code") != null
        && (bool?)Get(o, "enabled") != false
      )
        yield return o;
  }

  private static JToken Parse(string file) {
    try {
      return JToken.Parse(File.ReadAllText(file));
    } catch (Exception e) {
      Assert.Fail($"{file} does not parse: {e.Message}");
      throw;
    }
  }

  /// <summary>A property read ignoring case, as the game's loader reads it.</summary>
  internal static JToken? Get(JToken? token, string name) =>
    token is JObject o
      ? o.GetValue(name, StringComparison.OrdinalIgnoreCase)
      : null;

  private static Regex Glob(string pattern) =>
    new(
      "^" + string.Join(".*", pattern.Split('*').Select(Regex.Escape)) + "$"
    );

  private sealed class Definition {
    public string Domain { get; }
    public string Kind { get; }
    public string Code { get; }
    public List<(string Name, string[] States)> Groups { get; } = [];
    private readonly JObject _json;
    private readonly VariantCatalogue _owner;
    private List<(string Full, string[] Values)>? _variants;

    public Definition(
      string domain,
      string kind,
      JObject json,
      VariantCatalogue owner
    ) {
      Domain = domain;
      Kind = kind;
      Code = (string)Get(json, "code")!;
      _json = json;
      _owner = owner;
    }

    /// <summary>Every full code (without domain) with its value per group, expanded on first use.</summary>
    public List<(string Full, string[] Values)> Variants =>
      _variants ??= Expand();

    private List<(string, string[])> Expand() {
      foreach (JToken g in Get(_json, "variantgroups") ?? new JArray()) {
        var states = new List<string>();
        foreach (JToken s in Get(g, "states") ?? new JArray())
          states.Add((string)s!);
        var sources = new List<string>();
        if (Get(g, "loadFromProperties") is JToken one)
          sources.Add((string)one!);
        foreach (JToken s in Get(g, "loadFromPropertiesCombine") ?? new JArray())
          sources.Add((string)s!);
        foreach (string source in sources)
          states.AddRange(_owner.Property(Domain, source));
        Groups.Add(((string)Get(g, "code")!, [.. states]));
      }
      Regex[]? allowed = Get(_json, "allowedVariants")
        ?.Select(p => Glob((string)p!))
        .ToArray();
      Regex[]? skipped = Get(_json, "skipVariants")
        ?.Select(p => Glob((string)p!))
        .ToArray();

      IEnumerable<string[]> combos = [[]];
      foreach ((_, string[] states) in Groups)
        combos = combos.SelectMany(c => states.Select(s => (string[])[.. c, s]));
      var result = new List<(string, string[])>();
      foreach (string[] values in combos) {
        string full = string.Join("-", [Code, .. values]);
        if (allowed != null && !allowed.Any(r => r.IsMatch(full)))
          continue;
        if (skipped != null && skipped.Any(r => r.IsMatch(full)))
          continue;
        result.Add((full, values));
      }
      return result;
    }
  }
}
