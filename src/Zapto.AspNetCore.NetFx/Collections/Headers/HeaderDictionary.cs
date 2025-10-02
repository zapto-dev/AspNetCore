using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Zapto.AspNetCore.Collections;

internal sealed class HeaderDictionary : BaseHeaderDictionary
{
    private readonly Dictionary<string, StringValues> _dictionary;

    public HeaderDictionary()
    {
        _dictionary = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
    }

    public HeaderDictionary(IDictionary<string, StringValues> dictionary)
    {
        _dictionary = new Dictionary<string, StringValues>(dictionary, StringComparer.OrdinalIgnoreCase);
    }

    public HeaderDictionary(IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        _dictionary = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in collection)
        {
            _dictionary[item.Key] = item.Value;
        }
    }

    public HeaderDictionary(IEnumerable<KeyValuePair<string, string>> collection)
        : this(collection.Select(x => new KeyValuePair<string, StringValues>(x.Key, x.Value)))
    {
    }

    public override IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    public override void Clear()
    {
        _dictionary.Clear();
    }

    public override int Count => _dictionary.Count;

    public override bool IsReadOnly => false;

    public override void Add(string key, StringValues value)
    {
        _dictionary[key] = value;
    }

    public override bool ContainsKey(string key)
    {
        return _dictionary.ContainsKey(key);
    }

    public override bool Remove(string key)
    {
        return _dictionary.Remove(key);
    }

    public override bool TryGetValue(string key, out StringValues value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    public override ICollection<string> Keys => _dictionary.Keys;

    public override ICollection<StringValues> Values => _dictionary.Values;
}