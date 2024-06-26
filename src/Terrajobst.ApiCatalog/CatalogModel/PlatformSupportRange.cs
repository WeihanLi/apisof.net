﻿using System.Diagnostics;

namespace Terrajobst.ApiCatalog;

public readonly struct PlatformSupportRange : IEquatable<PlatformSupportRange>
{
    private readonly Version? _head;
    private readonly bool _headIsSupported;
    private readonly (Version Version, bool IsSupported)[] _tail;

    public PlatformSupportRange(IEnumerable<(Version Version, bool IsSupported)> versions)
    {
        ThrowIfNull(versions);

        var builder = new List<(Version Version, bool IsSupported)>();

        foreach (var (version, isSupported) in versions.OrderBy(t => t.Version)
                     .ThenBy(t => t.IsSupported))
        {
            if (builder.Count > 0)
            {
                var previous = builder[builder.Count - 1];
                if (previous.IsSupported == isSupported)
                {
                    // If there are no changes from the previous support condition,
                    // we can skip this version.
                    continue;
                }

                if (previous.Version == version &&
                    !previous.IsSupported &&
                    isSupported)
                {
                    // We have conflicting support policies. We exclude both entries.
                    builder.RemoveAt(builder.Count - 1);
                    continue;
                }
            }

            builder.Add((version, isSupported));
        }

        if (builder.Count == 0)
        {
            _head = null;
            _headIsSupported = false;
        }
        else
        {
            _head = builder[0].Version;
            _headIsSupported = builder[0].IsSupported;
            builder.RemoveAt(0);
        }

        _tail = builder.ToArray();
    }

    public bool IsEmpty => _head is null;

    public bool AllVersions => _head is not null &&
                               _head.Major == 0 &&
                               _head.Minor == 0 &&
                               _head.Build == 0 &&
                               _head.Revision == 0 &&
                               _tail.Length == 0;

    public bool IsAllowList => _headIsSupported;

    public IEnumerable<(Version? StartInclusive, Version? EndExclusive)> GetVersions()
    {
        if (IsEmpty)
            yield break;

        var headEnd = _tail.Length > 0
            ? _tail[0].Version
            : null;
        yield return (_head, headEnd);

        for (var i = 0; i < _tail.Length; i++)
        {
            var nextVersion = i < _tail.Length - 1
                ? _tail[i + 1].Version
                : null;

            var (v, supported) = _tail[i];

            if (supported == _headIsSupported)
                yield return (v, nextVersion);
        }
    }

    public bool IsSupported(Version version)
    {
        ThrowIfNull(version);

        for (var i = 0; i < Count; i++)
        {
            var current = this[i];
            var next = i == Count - 1 ? ((Version? Version, bool IsSupported)?)null : this[i + 1];

            if (current.Version > version)
                break;

            if (next is null || next.Value.Version > version)
                return current.IsSupported;
        }

        return !IsAllowList;
    }

    public (Version? Version, bool IsSupported) this[int index]
    {
        get
        {
            if (IsEmpty || index - 1 >= _tail.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
                return (_head, _headIsSupported);

            return _tail[index - 1];
        }
    }

    public int Count => IsEmpty ? 0 : _tail.Length + 1;

    public bool Equals(PlatformSupportRange other)
    {
        return Equals(_head, other._head) &&
               _headIsSupported == other._headIsSupported &&
               _tail.SequenceEqual(other._tail);
    }

    public override bool Equals(object? obj)
    {
        return obj is PlatformSupportRange other &&
               Equals(other);
    }

    public override int GetHashCode()
    {
        var builder = new HashCode();
        builder.Add(_head);
        builder.Add(_headIsSupported);

        foreach (var (v, s) in _tail)
        {
            builder.Add(v);
            builder.Add(s);
        }

        return builder.ToHashCode();
    }

    public static bool operator ==(PlatformSupportRange left, PlatformSupportRange right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PlatformSupportRange left, PlatformSupportRange right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        var versions = GetVersions();
        return string.Join(", ", versions.Select(t => Prettify(t.StartInclusive, t.EndExclusive)));

        static string Prettify(Version? startInclusive, Version? endExclusive)
        {
            if (startInclusive is null && endExclusive is null)
                return "any";

            if (startInclusive is null)
            {
                Debug.Assert(endExclusive is not null);
                return $"< {FormatVersion(endExclusive)}";
            }

            if (endExclusive is null)
            {
                Debug.Assert(startInclusive is not null);
                return $">= {FormatVersion(startInclusive)}";
            }

            return $"{FormatVersion(startInclusive)} - {FormatVersion(endExclusive)}";
        }
    }

    private static string FormatVersion(Version version)
    {
        if (version.Revision == 0)
        {
            if (version.Build == 0)
            {
                if (version.Minor == 0)
                    return $"{version.Major}";

                return $"{version.Major}.{version.Minor}";
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}