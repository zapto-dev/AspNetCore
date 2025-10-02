using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Collections.NameValue;

internal sealed class NameValueFormCollection : BaseNameValueDictionary, IFormCollection
{
	public NameValueFormCollection(NameValueCollection nameValueCollection, IFormFileCollection files)
		: base(nameValueCollection)
	{
		Files = files;
	}

	public NameValueFormCollection(NameValueCollection nameValueCollection)
		: this(nameValueCollection, EmptyFormFileCollection.Instance)
	{
	}

	public NameValueFormCollection()
	{
		Files = EmptyFormFileCollection.Instance;
	}

	public IFormFileCollection Files { get; private set; }

	public void SetFormFileCollection(IFormFileCollection files)
	{
		Files = files;
	}

	public override void Reset()
	{
		base.Reset();
		Files = EmptyFormFileCollection.Instance;
	}
}
