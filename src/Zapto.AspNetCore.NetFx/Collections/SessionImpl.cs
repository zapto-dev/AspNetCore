using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.SessionState;
using Microsoft.AspNetCore.Http;

namespace Zapto.AspNetCore.Collections;

internal class SessionImpl : ISession
{
    private AspNetContext _context = null!;

    public void SetSession(AspNetContext session)
    {
        _context = session;
    }

    public void Reset()
    {
        _context = null!;
    }

    public bool IsAvailable => true;

    public string Id => _context.Session.SessionID;

    public IEnumerable<string> Keys => _context.Session.Keys.Cast<string>();

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public bool TryGetValue(string key, out byte[]? value)
    {
        value = (byte[]?)_context.Session[key];
        return value != null;
    }

    public void Set(string key, byte[] value)
    {
        _context.Session[key] = value;
    }

    public void Remove(string key)
    {
        _context.Session.Remove(key);
    }

    public void Clear()
    {
        _context.Session.Clear();
    }
}
