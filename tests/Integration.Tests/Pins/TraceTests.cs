using System;
using System.IO;
using Xunit;

namespace Integration.Tests.Pins;

/// <summary>
/// How a trace is recorded against the committed one, in a scratch folder: written when absent or
/// named by the write variable, otherwise set beside it as a received file.
/// </summary>
public class TraceTests : IDisposable {
  private const string Name = "scene";

  private readonly string _folder = Path.Combine(
    Path.GetTempPath(),
    "legacy-trace-" + Guid.NewGuid().ToString("N")
  );

  private string Committed => Path.Combine(_folder, Name + ".trace");

  private string Received => Path.Combine(_folder, Name + ".received.trace");

  public void Dispose() {
    if (Directory.Exists(_folder))
      Directory.Delete(_folder, recursive: true);
  }

  private static Trace Scene(string amount) =>
    new Trace(Name).Line(0, "amount=" + amount);

  #region Save

  [Fact]
  public void A_trace_with_no_committed_file_is_written_as_the_committed_trace() {
    string text = Scene("100").Save(_folder, write: false);

    Assert.Equal(text, File.ReadAllText(Committed));
    Assert.False(File.Exists(Received));
  }

  [Fact]
  public void A_differing_trace_leaves_the_committed_file_and_writes_a_received_one() {
    Scene("77").Save(_folder, write: false);

    string text = Scene("100").Save(_folder, write: false);

    Assert.Equal(Scene("77").Text, File.ReadAllText(Committed));
    Assert.Equal(text, File.ReadAllText(Received));
  }

  [Fact]
  public void A_differing_trace_named_for_writing_replaces_the_committed_file() {
    Scene("77").Save(_folder, write: false);
    Scene("100").Save(_folder, write: false);

    string text = Scene("100").Save(_folder, write: true);

    Assert.Equal(text, File.ReadAllText(Committed));
    Assert.False(File.Exists(Received));
  }

  [Fact]
  public void A_matching_trace_deletes_a_stale_received_file() {
    Scene("77").Save(_folder, write: false);
    Scene("100").Save(_folder, write: false);

    Scene("77").Save(_folder, write: false);

    Assert.Equal(Scene("77").Text, File.ReadAllText(Committed));
    Assert.False(File.Exists(Received));
  }

  #endregion

  #region Write variable

  [Theory]
  [InlineData("1", true)]
  [InlineData("molten-rate, scene", true)]
  [InlineData("molten-rate", false)]
  [InlineData("", false)]
  [InlineData(null, false)]
  public void The_write_variable_names_every_trace_or_a_list(
    string? value,
    bool writes
  ) => Assert.Equal(writes, Trace.Writes(value, Name));

  #endregion
}
