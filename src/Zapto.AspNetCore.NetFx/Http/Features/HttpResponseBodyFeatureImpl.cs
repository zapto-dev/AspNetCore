using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Zapto.AspNetCore.Http.Features;

internal class HttpResponseBodyFeatureImpl : IHttpResponseBodyFeature
{
    private AspNetResponse _response = null!;
    private PipeWriter? _writer;

    public Stream Stream => _response.OutputStream;

    public PipeWriter Writer => _writer ??= PipeWriter.Create(_response.OutputStream);

    public void SetHttpResponse(AspNetResponse response)
    {
        _response = response;
    }

    public void Reset()
    {
        _writer = null;
        _response = null!;
    }

    public void DisableBuffering()
    {
        _response.BufferOutput = false;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        if (offset == 0 && !count.HasValue)
        {
            _response.TransmitFile(path);
        }
        else if (count.HasValue)
        {
            _response.TransmitFile(path, offset, count.Value);
        }
        else
        {
            var fileInfo = new FileInfo(path);
            _response.TransmitFile(path, offset, fileInfo.Length - offset);
        }

        return Task.CompletedTask;
    }

    public Task CompleteAsync()
    {
        _response.Flush();
        return Task.CompletedTask;
    }
}
