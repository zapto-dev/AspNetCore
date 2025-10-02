using System;
using System.IO;
using System.Threading.Tasks;
using Zapto.AspNetCore.Collections.Cookies;
using Zapto.AspNetCore.Collections.NameValue;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Http;

internal class HttpResponseImpl(HttpContextImpl httpContext) : HttpResponse
{
    private AspNetResponse _httpResponse = null!;
    private readonly NameValueHeaderDictionary _headers = new();
    private readonly ResponseCookiesImpl _cookies = new();

    private Stream _bodyValue;

    public void SetHttpResponse(AspNetResponse httpResponse)
    {
        _httpResponse = httpResponse;
        _headers.SetNameValueCollection(httpResponse.Headers);
        _cookies.SetHttpCookieCollection(httpResponse.Cookies);
        _bodyValue = httpResponse.OutputStream;
    }

    public void Reset()
    {
        _headers.Reset();
        _cookies.Reset();
        _httpResponse = null!;
        _bodyValue = null!;
    }

    public override void OnStarting(Func<object, Task> callback, object state)
    {
        throw new NotSupportedException("This method is not supported.");
    }

    public override void OnCompleted(Func<object, Task> callback, object state)
    {
        throw new NotSupportedException("This method is not supported.");
    }

    public override void Redirect(string location, bool permanent)
    {
        _httpResponse.Redirect(location, permanent);
    }

    public override HttpContext HttpContext => httpContext;

    public override int StatusCode
    {
        get => _httpResponse.StatusCode;
        set => _httpResponse.StatusCode = value;
    }

    public override IHeaderDictionary Headers => _headers;

    public override Stream Body
    {
        get => _bodyValue;
        set => _bodyValue = value;
    }

    public override long? ContentLength
    {
        get => long.TryParse(_httpResponse.Headers["Content-Length"], out var value) ? value : null;
        set => _httpResponse.Headers["Content-Length"] = value?.ToString();
    }

    public override string ContentType
    {
        get => _httpResponse.Headers["Content-Type"];
        set => _httpResponse.Headers["Content-Type"] = value;
    }

    public override IResponseCookies Cookies => _cookies;

    public override bool HasStarted => _httpResponse.HeadersWritten;
}
