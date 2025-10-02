using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Collections;

internal sealed class EmptyFormFileCollection : IFormFileCollection
{
	public static readonly EmptyFormFileCollection Instance = new();

	public IEnumerator<IFormFile> GetEnumerator()
	{
		return Enumerable.Empty<IFormFile>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public int Count => 0;

	public IFormFile this[int index] => throw new IndexOutOfRangeException();

	public IFormFile? this[string name] => null;

	public IFormFile? GetFile(string name)
	{
		return null;
	}

	public IReadOnlyList<IFormFile> GetFiles(string name)
	{
		return Array.Empty<IFormFile>();
	}
}
