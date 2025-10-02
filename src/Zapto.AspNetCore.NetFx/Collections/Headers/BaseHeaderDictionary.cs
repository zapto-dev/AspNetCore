using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Zapto.AspNetCore.Collections;

public abstract class BaseHeaderDictionary : IHeaderDictionary
{
    public abstract IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public virtual void Add(KeyValuePair<string, StringValues> item) => Add(item.Key, item.Value);

    public abstract void Clear();

    public bool Contains(KeyValuePair<string, StringValues> item) => TryGetValue(item.Key, out var value) && value == item.Value;

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        if (TryGetValue(item.Key, out var value) && value == item.Value)
        {
            return Remove(item.Key);
        }

        return false;
    }

    public abstract int Count { get; }
    public abstract bool IsReadOnly { get; }

    public abstract void Add(string key, StringValues value);

    public abstract bool ContainsKey(string key);

    public abstract bool Remove(string key);

    public abstract bool TryGetValue(string key, out StringValues value);

    public StringValues this[string key]
    {
        get => TryGetValue(key, out var value) ? value : StringValues.Empty;
        set => Add(key, value);
    }

    public abstract ICollection<string> Keys { get; }

    public abstract ICollection<StringValues> Values { get; }

    public string? ContentType
    {
        get => GetSingleValue("Content-Type");
        set => SetSingleValue("Content-Type", value);
    }

    public long? ContentLength
    {
        get => long.TryParse(GetSingleValue("Content-Length"), out var value) ? value : null;
        set => SetSingleValue("Content-Length", value?.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string? GetSingleValue(string key)
    {
        return TryGetValue(key, out var values) && values.Count > 0 ? values[0] : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSingleValue(string key, string? value)
    {
        if (value is null)
        {
            Remove(key);
        }
        else
        {
            this[key] = value;
        }
    }
}