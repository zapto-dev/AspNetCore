using System.Collections.Specialized;

namespace Zapto.AspNetCore.Collections.NameValue;

internal sealed class NameValueDictionary : BaseNameValueDictionary
{
    public NameValueDictionary()
    {
    }

    public NameValueDictionary(NameValueCollection nameValueCollection) : base(nameValueCollection)
    {
    }
}
