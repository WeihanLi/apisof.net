using System.Collections;

namespace Terrajobst.UsageCrawling.Storage;

internal sealed class IdMap<T> : IEnumerable<(int Id, T Value)>
    where T : notnull
{
    private readonly Dictionary<T, int> _idByValue;
    private readonly Dictionary<int, T> _valueById = new();
    private int _largestId;

    public IdMap(IEqualityComparer<T>? comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        _idByValue = new Dictionary<T, int>(comparer);
    }

    public void Add(int id, T value)
    {
        _idByValue.Add(value, id);
        _valueById.Add(id, value);
        _largestId = Math.Max(_largestId, id);
    }

    public int Add(T value)
    {
        var id = _largestId + 1;
        Add(id, value);
        return id;
    }

    public int GetOrAdd(T value)
    {
        if (!_idByValue.TryGetValue(value, out var id))
            id = Add(value);

        return id;
    }

    public int GetId(T value)
    {
        return _idByValue[value];
    }

    public bool TryGetId(T value, out int id)
    {
        return _idByValue.TryGetValue(value, out id);
    }

    public T GetValue(int id)
    {
        return _valueById[id];
    }

    public bool Contains(T value)
    {
        return _idByValue.ContainsKey(value);
    }

    public void RemoveId(int id)
    {
        if (!_valueById.Remove(id, out var value))
            return;

        _idByValue.Remove(value);
    }

    public IReadOnlyCollection<int> Keys => _valueById.Keys;

    public IReadOnlyCollection<T> Values => _idByValue.Keys;

    public int Count => _valueById.Count;

    public IEnumerator<(int Id, T Value)> GetEnumerator()
    {
        foreach (var (k, v) in _valueById)
            yield return (k, v);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}