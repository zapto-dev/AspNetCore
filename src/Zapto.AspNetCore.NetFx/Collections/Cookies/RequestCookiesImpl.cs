using System.Collections;
using System.Collections.Generic;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Collections.Cookies;

internal sealed class RequestCookiesImpl : IRequestCookieCollection
{
    private HttpCookieCollection _httpCookieCollection = null!;

    public void SetHttpCookieCollection(HttpCookieCollection httpCookieCollection)
    {
        _httpCookieCollection = httpCookieCollection;
    }

    public void Reset()
    {
        _httpCookieCollection = null!;
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (string key in _httpCookieCollection)
        {
            yield return new KeyValuePair<string, string>(key, _httpCookieCollection[key]?.Value ?? "");
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _httpCookieCollection.Count;
    public ICollection<string> Keys => _httpCookieCollection.AllKeys;
    public bool ContainsKey(string key)
    {
        return _httpCookieCollection[key] != null;
    }

    public bool TryGetValue(string key, out string? value)
    {
        value = _httpCookieCollection[key]?.Value;
        return value != null;
    }

    public string? this[string key] => _httpCookieCollection[key]?.Value;
}
