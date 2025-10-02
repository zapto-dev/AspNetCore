using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;

namespace Zapto.AspNetCore.Collections;

internal sealed class AspNetCoreFormFileCollection : IFormFileCollection
{
    private static readonly ObjectPool<FormFile> FormFilePool = new DefaultObjectPool<FormFile>(new DefaultPooledObjectPolicy<FormFile>());
    private HttpFileCollection? _formCollection;
    private readonly List<FormFile> _formFiles = new();

    public void Reset()
    {
        foreach (var file in _formFiles)
        {
            file.Reset();
            FormFilePool.Return(file);
        }

        _formFiles.Clear();
        _formCollection = null!;
    }

    public void SetHttpFileCollection(HttpFileCollection collection)
    {
        _formCollection = collection;
    }

    private void Load()
    {
        if (_formCollection is null)
        {
            throw new InvalidOperationException("Form collection is not set.");
        }

        var count = _formCollection.Count;

        for (var i = 0; i < count; i++)
        {
            var formFile = _formCollection[i]!;
            var file = FormFilePool.Get();
            file.SetFormFile(formFile);
            _formFiles.Add(file);
        }
    }

    public IEnumerator<IFormFile> GetEnumerator()
    {
        Load();
        return _formFiles.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _formCollection?.Count ?? 0;

    public IFormFile this[int index]
    {
        get
        {
            Load();
            return _formFiles[index];
        }
    }

    public IFormFile? this[string name] => GetFile(name);

    public IFormFile? GetFile(string name)
    {
        Load();

        foreach (var file in _formFiles)
        {
            if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
            {
                return file;
            }
        }

        return null;
    }

    public IReadOnlyList<IFormFile> GetFiles(string name)
    {
        Load();

        var files = new List<IFormFile>();

        foreach (var file in _formFiles)
        {
            if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
            {
                files.Add(file);
            }
        }

        return files;
    }

    private class FormFile : IFormFile
    {
        private readonly IHeaderDictionary _headers = new HeaderDictionary();
        private HttpPostedFile? _formFile;

        public void SetFormFile(HttpPostedFile formFile)
        {
            _formFile = formFile;
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = new CancellationToken())
        {
            return _formFile!.InputStream.CopyToAsync(target, 81920, cancellationToken);
        }

        public string? ContentType => _formFile?.ContentType;
        public string ContentDisposition { get; set; }

        public string? FileName => _formFile?.FileName;

        public IHeaderDictionary Headers => _headers;

        public long Length => _formFile?.ContentLength ?? 0;

        public string? Name => _formFile?.FileName;

        public Stream OpenReadStream()
        {
            return _formFile!.InputStream;
        }

        public void CopyTo(Stream target)
        {
            _formFile!.InputStream.CopyTo(target);
        }

        public void Reset()
        {
            _formFile = null!;
            _headers.Clear();
        }
    }
}
