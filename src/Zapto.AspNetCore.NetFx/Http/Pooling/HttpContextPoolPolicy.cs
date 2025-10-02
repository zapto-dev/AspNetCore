using Microsoft.Extensions.ObjectPool;

namespace Zapto.AspNetCore.Http;

internal class HttpContextPoolPolicy : IPooledObjectPolicy<HttpContextImpl>
{
    public static readonly HttpContextPoolPolicy Instance = new();

    public HttpContextImpl Create()
    {
        return new HttpContextImpl();
    }

    public bool Return(HttpContextImpl obj)
    {
        obj.Reset();
        return true;
    }
}
