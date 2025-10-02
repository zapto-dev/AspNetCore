using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Extensions.Primitives;

namespace Zapto.AspNetCore.Collections.NameValue;

internal sealed class NameValueHeaderDictionary : BaseHeaderDictionary
{
    private readonly NameValueDictionary _nameValueCollection = new();

    public void SetNameValueCollection(NameValueCollection nameValueCollection)
    {
        _nameValueCollection.SetNameValueCollection(nameValueCollection);
    }

    public void Reset()
    {
        _nameValueCollection.Reset();
    }

    public override IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
    {
        return _nameValueCollection.GetEnumerator();
    }

    public override void Clear()
    {
        _nameValueCollection.Clear();
    }

    public override int Count => _nameValueCollection.Count;

    public override bool IsReadOnly => _nameValueCollection.IsReadOnly;

    public override void Add(string key, StringValues value)
    {
        _nameValueCollection.Add(key, value);
    }

    public override bool ContainsKey(string key)
    {
        return _nameValueCollection.ContainsKey(key);
    }

    public override bool Remove(string key)
    {
        return _nameValueCollection.Remove(key);
    }

    public override bool TryGetValue(string key, out StringValues value)
    {
        return _nameValueCollection.TryGetValue(key, out value);
    }

    public override ICollection<string> Keys => _nameValueCollection.Keys;

    public override ICollection<StringValues> Values => _nameValueCollection.Values;
}
