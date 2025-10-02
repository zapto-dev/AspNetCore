using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Zapto.AspNetCore.Collections.NameValue;

internal abstract class BaseNameValueDictionary : IDictionary<string, StringValues>, IQueryCollection
{
    private NameValueCollection? _nameValueCollection;

    public BaseNameValueDictionary()
    {
    }

    public BaseNameValueDictionary(NameValueCollection nameValueCollection)
    {
        _nameValueCollection = nameValueCollection;
    }

    public void SetQueryString(string query)
    {
        NameValueCollection = HttpUtility.ParseQueryString(query);
    }

    public void SetNameValueCollection(NameValueCollection nameValueCollection)
    {
        NameValueCollection = nameValueCollection;
    }

    protected NameValueCollection NameValueCollection
    {
        get => _nameValueCollection ??= new NameValueCollection();
        set => _nameValueCollection = value;
    }

    public virtual void Reset()
    {
        _nameValueCollection = null!;
    }

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _nameValueCollection?.AllKeys
                   .Select(key => new KeyValuePair<string, StringValues>(key!, NameValueCollection[key!]))
                   .GetEnumerator()
               ?? Enumerable.Empty<KeyValuePair<string, StringValues>>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(KeyValuePair<string, StringValues> item)
    {
        NameValueCollection.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _nameValueCollection?.Clear();
    }

    public bool Contains(KeyValuePair<string, StringValues> item)
    {
        return _nameValueCollection != null &&
               _nameValueCollection.AllKeys.Contains(item.Key) &&
               _nameValueCollection[item.Key] == item.Value;
    }

    public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
    {
        if (_nameValueCollection == null)
        {
            return;
        }

        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public bool Remove(KeyValuePair<string, StringValues> item)
    {
        if (_nameValueCollection == null)
        {
            return false;
        }

        if (Contains(item))
        {
            NameValueCollection.Remove(item.Key);
            return true;
        }

        return false;
    }

    public int Count => _nameValueCollection?.Count ?? 0;

    public bool IsReadOnly => false;
    public bool ContainsKey(string key)
    {
        return _nameValueCollection?.AllKeys.Contains(key) ?? false;
    }

    public void Add(string key, StringValues value)
    {
        NameValueCollection.Add(key, value);
    }

    public bool Remove(string key)
    {
        if (_nameValueCollection == null)
        {
            return false;
        }

        if (ContainsKey(key))
        {
            NameValueCollection.Remove(key);
            return true;
        }

        return false;
    }

    public bool TryGetValue(string key, out StringValues value)
    {
        if (_nameValueCollection == null)
        {
            value = default;
            return false;
        }

        if (ContainsKey(key))
        {
            value = NameValueCollection[key];
            return true;
        }

        value = default;
        return false;
    }

    public StringValues this[string key]
    {
        get => _nameValueCollection?[key] ?? default;
        set => NameValueCollection[key] = value;
    }

    public ICollection<string> Keys => NameValueCollection.AllKeys!;

    public ICollection<StringValues> Values => NameValueCollection.AllKeys
        .Select(key => (StringValues)NameValueCollection[key])
        .ToList();
}
