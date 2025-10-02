using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.WebSockets;

namespace Zapto.AspNetCore.Http;

internal class WebSocketManagerImpl : WebSocketManager
{
    private AspNetContext _context = null!;

    // Static reflection members - initialized once at startup
    private static readonly PropertyInfo WorkerRequestProperty;
    private static readonly MethodInfo SuppressSendResponseNotificationsMethod;
    private static readonly MethodInfo GetWebSocketContextMethod;
    private static readonly Type WebSocketPipeType;
    private static readonly Type WorkerRequestType;
    private static readonly FieldInfo PerfCountersInstanceField;
    private static readonly ConstructorInfo WebSocketPipeConstructor;
    private static readonly ConstructorInfo AspNetWebSocketConstructor;

    static WebSocketManagerImpl()
    {
        // Initialize all reflection members once and validate they exist
        var aspNetContextType = typeof(AspNetContext);
        WorkerRequestProperty = aspNetContextType.GetProperty("WorkerRequest", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the internal WorkerRequest property on AspNetContext. This may indicate an incompatible version of ASP.NET.");

        // Get WorkerRequest type to find its methods
        WorkerRequestType = typeof(AspNetContext).Assembly.GetType("System.Web.Hosting.IIS7WorkerRequest")
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the internal IWorkerRequest type. This may indicate an incompatible version of ASP.NET.");

        SuppressSendResponseNotificationsMethod = WorkerRequestType.GetMethod("SuppressSendResponseNotifications", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the SuppressSendResponseNotifications method on the WorkerRequest type. This feature requires a compatible version of ASP.NET Framework.");

        GetWebSocketContextMethod = WorkerRequestType.GetMethod("GetWebSocketContext", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the GetWebSocketContext method on the WorkerRequest type. WebSocket support may not be available in this ASP.NET Framework version.");

        // Get WebSocketPipe type and constructor
        WebSocketPipeType = typeof(AspNetWebSocket).Assembly.GetType("System.Web.WebSockets.WebSocketPipe")
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the internal WebSocketPipe type. This indicates an incompatible version of System.Web or missing WebSocket components.");

        WebSocketPipeConstructor = WebSocketPipeType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault()
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find a suitable constructor for WebSocketPipe. The internal API structure may have changed in this version of ASP.NET Framework.");

        // Get PerfCounters type and Instance field
        var perfCountersType = typeof(AspNetWebSocket).Assembly.GetType("System.Web.PerfCounters")
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the internal PerfCounters type. Performance counter integration may not be available in this ASP.NET Framework version.");

        PerfCountersInstanceField = perfCountersType.GetField("Instance", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find the PerfCounters.Instance field. Performance counter support may be unavailable or the internal API has changed.");

        // Get AspNetWebSocket constructor
        AspNetWebSocketConstructor = typeof(AspNetWebSocket).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault()
            ?? throw new InvalidOperationException("WebSocket initialization failed: Could not find a suitable constructor for AspNetWebSocket. This may indicate an incompatible version of ASP.NET Framework or missing WebSocket support.");
    }

    public void SetContext(AspNetContext context)
    {
        _context = context;
    }

    public void Reset()
    {
        _context = null!;
    }

    public override Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
    {
        if (!_context.IsWebSocketRequest)
            throw new InvalidOperationException("The request is not a valid WebSocket request.");

        // Perform manual WebSocket upgrade
        var webSocketKey = _context.Request.Headers["Sec-WebSocket-Key"];
        if (string.IsNullOrEmpty(webSocketKey))
            throw new InvalidOperationException("Missing Sec-WebSocket-Key header");

        // Validate the WebSocket key format according to RFC 6455
        if (!IsValidWebSocketKey(webSocketKey))
            throw new InvalidOperationException("Invalid Sec-WebSocket-Key header format");

        // Generate accept key
        var acceptKey = GenerateWebSocketAcceptKey(webSocketKey);

        // Disable response buffering for real-time delivery
        _context.Response.BufferOutput = false;

        // Clear any existing content type and content length headers to prevent IIS from adding content
        _context.Response.ContentType = null;
        _context.Response.Headers.Remove("Content-Length");
        _context.Response.Headers.Remove("Content-Type");

        // Clear the response buffer to prevent any existing content from being sent
        _context.Response.Clear();

        // Send WebSocket upgrade response
        _context.Response.Status = "101 Switching Protocols";
        _context.Response.Headers.Add("Upgrade", "websocket");
        _context.Response.Headers.Add("Connection", "Upgrade");
        _context.Response.Headers.Add("Sec-WebSocket-Accept", acceptKey);

        if (!string.IsNullOrEmpty(subProtocol))
        {
            _context.Response.Headers.Add("Sec-WebSocket-Protocol", subProtocol);
        }

        _context.Response.Flush();

        var workerRequest = WorkerRequestProperty.GetValue(_context);
        if (workerRequest == null) throw new InvalidOperationException("Could not retrieve WorkerRequest from ASP.NET context.");

        // Check if the current worker process is IIS7WorkerRequest
        if (!WorkerRequestType.IsInstanceOfType(workerRequest))
            throw new InvalidOperationException("WebSocket upgrade is only supported with IIS7WorkerRequest. The current hosting environment may not support WebSockets.");

        SuppressSendResponseNotificationsMethod.Invoke(workerRequest, null);

        var webSocketContext = GetWebSocketContextMethod.Invoke(workerRequest, null);
        if (webSocketContext == null) throw new InvalidOperationException("Failed to obtain WebSocketContext from WorkerRequest.");

        var perfCountersInstance = PerfCountersInstanceField.GetValue(null);
        var webSocketPipe = WebSocketPipeConstructor.Invoke([webSocketContext, perfCountersInstance]);
        var aspNetWebSocket = (AspNetWebSocket)AspNetWebSocketConstructor.Invoke([webSocketPipe, subProtocol]);

        return Task.FromResult<WebSocket>(aspNetWebSocket);
    }

    private static string GenerateWebSocketAcceptKey(string webSocketKey)
    {
        const string webSocketMagicString = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var combined = webSocketKey + webSocketMagicString;
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    private static bool IsValidWebSocketKey(string webSocketKey)
    {
        // RFC 6455: The key must be a base64-encoded value that decodes to 16 bytes
        if (string.IsNullOrWhiteSpace(webSocketKey))
            return false;

        Span<byte> buffer = stackalloc byte[16];
        var utf8Bytes = Encoding.UTF8.GetBytes(webSocketKey);

        var result = Base64.DecodeFromUtf8(utf8Bytes, buffer, out _, out var bytesWritten);
        return result == OperationStatus.Done && bytesWritten == 16;
    }

    public override bool IsWebSocketRequest => _context.IsWebSocketRequest;
    public override IList<string> WebSocketRequestedProtocols => Array.Empty<string>();
}
