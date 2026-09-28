using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ExpandedLib.Generators;

/// <summary>Value-equatable wrapper over <see cref="ImmutableArray{T}"/> so generator models compare
/// by content - lets the incremental pipeline cache correctly across edits.</summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
  where T : IEquatable<T> {
  private readonly ImmutableArray<T> _array;

  public EquatableArray(ImmutableArray<T> array) => _array = array;

  public ReadOnlySpan<T> AsSpan() =>
    _array.IsDefault ? ReadOnlySpan<T>.Empty : _array.AsSpan();

  public bool Equals(EquatableArray<T> other) =>
    AsSpan().SequenceEqual(other.AsSpan());

  public override bool Equals(object? obj) =>
    obj is EquatableArray<T> other && Equals(other);

  public override int GetHashCode() {
    unchecked {
      int hash = 17;
      foreach (var item in AsSpan())
        hash = hash * 31 + EqualityComparer<T>.Default.GetHashCode(item);
      return hash;
    }
  }
}
