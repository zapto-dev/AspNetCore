using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zapto.AspNetCore.Collections;
using Zapto.AspNetCore.Collections.Cookies;
using Zapto.AspNetCore.Collections.NameValue;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Http;

internal class HttpRequestImpl : HttpRequest
{
    private AspNetContext _aspContext;
    private AspNetRequest _aspRequest;
    private readonly AspNetCoreFormFileCollection _formFiles = new();
    private readonly NameValueFormCollection _form = new();
    private readonly NameValueDictionary _query = new();
    private readonly NameValueHeaderDictionary _headers = new();
    private readonly RequestCookiesImpl _cookies = new();
    private readonly HttpContextImpl _httpContext;

    private IQueryCollection _queryValue;
    private IRequestCookieCollection _cookiesValue;
    private IFormCollection _formValue;
    private QueryString _queryStringValue = QueryString.Empty;
    private long? _contentLengthValue;
    private string _httpMethodValue;
    private string _schemeValue;
    private HostString _hostValue;
    private Stream _bodyValue;

    public HttpRequestImpl(HttpContextImpl httpContext)
    {
        _httpContext = httpContext;
        _queryValue = _query;
        _cookiesValue = _cookies;
        _formValue = _form;
    }

    public void SetHttpRequest(AspNetContext context, AspNetRequest httpRequest)
    {
        _aspContext = context;
        _aspRequest = httpRequest;
        _formFiles.SetHttpFileCollection(httpRequest.Files);
        _form.SetNameValueCollection(httpRequest.Form);
        _query.SetNameValueCollection(httpRequest.QueryString);
        _headers.SetNameValueCollection(httpRequest.Headers);
        _cookies.SetHttpCookieCollection(httpRequest.Cookies);
        _queryStringValue = string.IsNullOrEmpty(httpRequest.Url.Query) ? QueryString.Empty : new QueryString(httpRequest.Url.Query);
        _httpMethodValue = httpRequest.HttpMethod;
        _contentLengthValue = httpRequest.ContentLength;
        _schemeValue = httpRequest.Url.Scheme;
        _hostValue = new HostString(httpRequest.Url.Host, httpRequest.Url.Port);
    }

    public void Reset()
    {
        _aspContext = null!;
        _aspRequest = null!;
        _formFiles.Reset();
        _form.Reset();
        _query.Reset();
        _headers.Reset();
        _cookies.Reset();
        _queryValue = _query;
        _cookiesValue = _cookies;
        _formValue = _form;
        _contentLengthValue = null;
        _httpMethodValue = null!;
        _queryStringValue = QueryString.Empty;
        _schemeValue = null!;
        _hostValue = new HostString();
        _bodyValue = null!;
    }

    public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult(_formValue);
    }

    public override HttpContext HttpContext => _httpContext;

    public override string Method
    {
        get => _httpMethodValue;
        set => _httpMethodValue = value;
    }

    public override string Scheme
    {
        get => _schemeValue;
        set => _schemeValue = value;
    }

    public override bool IsHttps
    {
        get => _aspRequest.IsSecureConnection;
        set => throw new NotSupportedException("Changing the IsHttps property is not supported.");
    }

    public override HostString Host
    {
        get => _hostValue;
        set => _hostValue = value;
    }

    public override PathString PathBase
    {
        get => new(_aspRequest.ApplicationPath);
        set => throw new NotSupportedException("Changing the PathBase property is not supported.");
    }

    public override PathString Path
    {
        get => _aspRequest.Path;
        set => _aspContext.RewritePath(value);
    }

    public override QueryString QueryString
    {
        get => _queryStringValue;
        set => _queryStringValue = value;
    }

    public override IQueryCollection Query
    {
        get => _queryValue;
        set => _queryValue = value;
    }

    public override string Protocol
    {
        get => _aspRequest.ServerVariables["SERVER_PROTOCOL"];
        set => _aspRequest.ServerVariables["SERVER_PROTOCOL"] = value;
    }

    public override IHeaderDictionary Headers => _headers;

    public override IRequestCookieCollection Cookies
    {
        get => _cookiesValue;
        set => _cookiesValue = value;
    }

    public override long? ContentLength
    {
        get => _contentLengthValue;
        set => _contentLengthValue = value;
    }

    public override string ContentType
    {
        get => _aspRequest.ContentType;
        set => _aspRequest.ContentType = value;
    }

    public override Stream Body
    {
        get => _bodyValue ??= _aspRequest.InputStream;
        set => _bodyValue = value;
    }

    public override bool HasFormContentType =>
        _aspRequest.ContentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ||
        _aspRequest.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

    public override IFormCollection Form
    {
        get => _formValue;
        set => _formValue = value;
    }
}
