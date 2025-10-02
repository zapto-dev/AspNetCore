using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Zapto.AspNetCore.Http;
using Zapto.AspNetCore.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace Zapto.AspNetCore;

public abstract class AspNetCoreModule : IHttpModule, IHttpAsyncHandler
{
    private static readonly ObjectPool<HttpContextImpl> HttpContextPool = new DefaultObjectPool<HttpContextImpl>(HttpContextPoolPolicy.Instance);
    private readonly SharedModuleContext _context;

    protected AspNetCoreModule()
    {
        _context = SharedModuleContext.GetContext(this);
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        foreach (var method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name != "Configure" || method.DeclaringType == typeof(AspNetCoreModule))
            {
                continue;
            }

            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                args[i] = parameters[i].ParameterType == typeof(IApplicationBuilder)
                    ? app
                    : app.ApplicationServices.GetService(parameters[i].ParameterType);
            }

            method.Invoke(this, args);
        }
    }

    /// <summary>
    /// Initialize the service provider.
    /// </summary>
    /// <param name="application">The application.</param>
    void IHttpModule.Init(HttpApplication? application)
    {
        if (application is null)
        {
            return;
        }

        var executeStack = new EventHandlerTaskAsyncHelper(ExecuteStackAsync);
        application.AddOnPreRequestHandlerExecuteAsync(executeStack.BeginEventHandler, executeStack.EndEventHandler);

        var finishStack = new EventHandlerTaskAsyncHelper(FinishStackAsync);
        application.AddOnEndRequestAsync(finishStack.BeginEventHandler, finishStack.EndEventHandler);
    }

    /// <summary>
    /// Execute the stack and finish it.
    /// </summary>
    /// <param name="context"></param>
    private async Task ExecuteAndFinishStackAsync(AspNetContext context)
    {
        try
        {
            await ExecuteStackAsync(context);
        }
        finally
        {
            await FinishStackAsync(context);
        }
    }

    /// <summary>
    /// Execute the stack.
    /// </summary>
    /// <param name="sender">The application.</param>
    /// <param name="e">The event arguments.</param>
    private async Task ExecuteStackAsync(object sender, EventArgs e)
    {
        if (sender is HttpApplication { Context: { } context })
        {
            await ExecuteStackAsync(context);
        }
    }

    /// <summary>
    /// Execute the stack.
    /// </summary>
    /// <param name="context">The ASP.NET context.</param>
    private async Task ExecuteStackAsync(AspNetContext context)
    {
        await _context.HostedServicesTask; // Wait for hosted services to start

        if (context.Items[typeof(HttpContextImpl)] is HttpContextImpl)
        {
            return;
        }

        var scope = _context.Host.Services.CreateAsyncScope();
        var httpContext = HttpContextPool.Get();

        context.Items[typeof(HttpContextImpl)] = httpContext;
        httpContext.SetContext(context, scope);

        var task = _context.Delegate(httpContext);
        httpContext.SetMiddlewareTask(task);

        await Task.WhenAny(task, httpContext.WaitForStackAsync());


        var didFinishStack = !context.Items.Contains(ApplicationBuilder.RequestUnhandledKey);

        if (context.Handler != null && context.Handler.GetType() == GetType())
        {
            if (!didFinishStack)
            {
                // We cannot switch handlers here, so we just set a 404 status code.
                context.Response.StatusCode = 404;
            }
        }

        if (didFinishStack)
        {
            context.ApplicationInstance.CompleteRequest();
        }
    }

    private async Task FinishStackAsync(object sender, EventArgs e)
    {
        if (sender is HttpApplication { Context: { } context })
        {
            await FinishStackAsync(context);
        }
    }

    private static async Task FinishStackAsync(AspNetContext context)
    {
        if (context.Items[typeof(HttpContextImpl)] is not HttpContextImpl httpContext)
        {
            return;
        }

        context.Items[typeof(HttpContextImpl)] = null;
        await httpContext.CompleteAsync();
        HttpContextPool.Return(httpContext);
    }

    /// <summary>
    /// Dispose the service provider and stop all hosted services.
    /// </summary>
    public void Dispose()
    {
    }

    void IHttpHandler.ProcessRequest(AspNetContext context)
    {
        var task = ExecuteAndFinishStackAsync(context);

        if (!task.IsCompleted)
        {
            task.GetAwaiter().GetResult();
        }
    }

    bool IHttpHandler.IsReusable => true;

    IAsyncResult IHttpAsyncHandler.BeginProcessRequest(AspNetContext context, AsyncCallback cb, object extraData)
    {
        var task = ExecuteAndFinishStackAsync(context);
        return AsyncFactory.ToBegin(task, cb, extraData);
    }

    void IHttpAsyncHandler.EndProcessRequest(IAsyncResult result)
    {
        AsyncFactory.ToEnd(result);
    }
}