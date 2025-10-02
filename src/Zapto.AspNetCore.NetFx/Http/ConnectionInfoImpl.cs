using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Http;

internal class ConnectionInfoImpl : ConnectionInfo
{
    private AspNetContext _context = null!;
    private IPAddress? _remoteIpAddress;
    private X509Certificate2 _certificate;

    public void SetContext(AspNetContext context)
    {
        _context = context;
    }

    public void Reset()
    {
        _context = null!;
        _remoteIpAddress = null;
        _certificate = null!;
    }

    public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = new()) => Task.FromResult(ClientCertificate);

    public override string Id
    {
        get => _context.Request.ClientCertificate.Subject;
        set => throw new NotSupportedException();
    }

    public override IPAddress RemoteIpAddress
    {
        get
        {
            if (_remoteIpAddress != null)
            {
                return _remoteIpAddress;
            }

            var ip = _context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
            {
                ip = _context.Request.ServerVariables["REMOTE_ADDR"];
            }
            else
            {
                var index = ip.IndexOf(',');

                if (index != -1)
                {
                    ip = ip.Substring(0, index);
                }
            }

            if (IPAddress.TryParse(ip, out var address))
            {
                _remoteIpAddress = address;
            }
            else
            {
                throw new InvalidOperationException("Could not get the remote IP address.");
            }

            return _remoteIpAddress;
        }
        set => _remoteIpAddress = value;
    }

    public override int RemotePort
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override IPAddress LocalIpAddress
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int LocalPort
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override X509Certificate2 ClientCertificate
    {
        get => _certificate ??= _context.Request.ClientCertificate.Certificate.Length > 0
            ? new X509Certificate2(_context.Request.ClientCertificate.Certificate)
            : null;
        set => throw new NotSupportedException();
    }
}
