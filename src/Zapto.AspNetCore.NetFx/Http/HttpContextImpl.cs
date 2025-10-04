using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zapto.AspNetCore.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Zapto.AspNetCore.Http;

internal class HttpContextImpl : HttpContext
{
    private FeatureCollection? _features;
    private AspNetContext _aspContext = null!;
    private readonly HttpRequestImpl _request;
    private readonly HttpResponseImpl _response;
    private readonly HashDictionary _items = new();
    private readonly ConnectionInfoImpl _connection = new();
    private readonly WebSocketManagerImpl _webSocketManager;
    private readonly SessionImpl _session = new();
    private IServiceProvider _requestServices = null!;
    private IDictionary<object, object?> _itemsValue;
    private ISession _sessionValue;
    private TaskCompletionSource<bool> _stackCompleteTcs = null!;
    private TaskCompletionSource<bool> _aspCompleteTcs = null!;
    private AsyncServiceScope _serviceScope;
    private Task _middlewareTask = null!;

    public HttpContextImpl()
    {
        _request = new HttpRequestImpl(this);
        _response = new HttpResponseImpl(this);
        _webSocketManager = new WebSocketManagerImpl();
        _itemsValue = _items;
        _sessionValue = _session;
    }

    public AspNetContext AspNetContext => _aspContext;

    public void SetContext(AspNetContext httpContext, AsyncServiceScope scope)
    {
        _aspContext = httpContext;
        _serviceScope = scope;
        _requestServices = scope.ServiceProvider;
        _request.SetHttpRequest(httpContext, httpContext.Request);
        _response.SetHttpResponse(httpContext.Response);
        _items.SetDictionary(httpContext.Items);
        _session.SetSession(httpContext);
        _webSocketManager.SetContext(httpContext);
        _connection.SetContext(httpContext);
        _stackCompleteTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _aspCompleteTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Reset()
    {
        _features = null;
        _itemsValue = _items;
        _connection.Reset();
        _request.Reset();
        _response.Reset();
        _items.Reset();
        _aspContext = null!;
        _webSocketManager.Reset();
        _session.Reset();
        _requestServices = null!;
        RequestAborted = CancellationToken.None;
        TraceIdentifier = null!;
        _sessionValue = _session;
        _stackCompleteTcs = null!;
        _aspCompleteTcs = null!;
        _middlewareTask = null!;
    }

    public override void Abort()
    {
        _aspContext.Request.Abort();
    }

    public override IFeatureCollection Features => _features ??= new FeatureCollection();
    public override HttpRequest Request => _request;
    public override HttpResponse Response => _response;
    public override ConnectionInfo Connection => _connection;
    public override WebSocketManager WebSockets => _webSocketManager;
    [Obsolete] public override AuthenticationManager Authentication => throw new NotSupportedException();

    public override ClaimsPrincipal User
    {
        get => (ClaimsPrincipal)_aspContext.User;
        set => _aspContext.User = value;
    }

    public override IDictionary<object, object?> Items
    {
        get => _itemsValue;
        set => _itemsValue = value;
    }

    public override IServiceProvider RequestServices
    {
        get => _requestServices;
        set => _requestServices = value;
    }

    public override CancellationToken RequestAborted { get; set; }

    public override string TraceIdentifier { get; set; } = null!;

    public override ISession Session
    {
        get => _sessionValue;
        set => _sessionValue = value;
    }

    public void SetMiddlewareTask(Task task)
    {
        _middlewareTask = task;
    }

    public async Task FinishStackAsync()
    {
        _stackCompleteTcs.SetResult(true);
        await _aspCompleteTcs.Task;
    }

    public async Task WaitForStackAsync()
    {
        await _stackCompleteTcs.Task;
    }

    public async Task CompleteAsync()
    {
        _aspCompleteTcs.SetResult(true);
        try
        {
            await _middlewareTask;
        }
        finally
        {
            await _serviceScope.DisposeAsync();
        }
    }
}
