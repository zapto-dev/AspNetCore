using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;

namespace Zapto.AspNetCore;

internal class SharedModuleContext : IRegisteredObject
{
    private static readonly ConcurrentDictionary<Type, Lazy<SharedModuleContext>> ModuleContexts = new();

    public static SharedModuleContext GetContext(AspNetCoreModule module)
    {
        return ModuleContexts.TryGetValue(module.GetType(), out var context)
            ? context.Value
            : CreateContext(module);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SharedModuleContext CreateContext(AspNetCoreModule module)
    {
        return ModuleContexts.GetOrAdd(module.GetType(), _ => new Lazy<SharedModuleContext>(
            () =>
            {
                var instance = new SharedModuleContext();
                HostingEnvironment.RegisterObject(instance);
                instance.Init(module);
                return instance;
            },
            isThreadSafe: true)).Value;
    }

    private SharedModuleContext()
    {
    }

    public readonly object InitLock = new();
    public bool IsInitialized;
    public IHost Host;
    public RequestDelegate Delegate;
    public Task HostedServicesTask = Task.CompletedTask;

    public void Stop(bool immediate)
    {
        StopHost();
        HostingEnvironment.UnregisterObject(this);
    }

    private void Init(AspNetCoreModule module)
    {
        lock (InitLock)
        {
            if (IsInitialized)
            {
                return;
            }

            IsInitialized = true;

            var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Args = [],
                ContentRootPath = HostingEnvironment.ApplicationPhysicalPath,
                EnvironmentName = HostingEnvironment.IsDevelopmentEnvironment ? "Development" : "Production",
                ApplicationName = HostingEnvironment.SiteName
            });

            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            hostBuilder.Services.TryAddSingleton(listener);
            hostBuilder.Services.TryAddSingleton<DiagnosticSource>(listener);
            hostBuilder.Services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            module.ConfigureServices(hostBuilder.Services);

            var host = hostBuilder.Build();
            Host = host;

            var builder = new ApplicationBuilder(host.Services);
            var startupFilters = host.Services.GetService<IEnumerable<IStartupFilter>>();
            var configure = module.Configure;

            if (startupFilters != null)
            {
                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }
            }

            configure(builder);
            Delegate = builder.Build();

            StartHostedServicesInBackground(host);
        }
    }

    /// <summary>
    /// Start hosted services in the background.
    /// </summary>
    /// <param name="host">The host.</param>
    private void StartHostedServicesInBackground(IHost host)
    {
        var tcs = new TaskCompletionSource<bool>();

        HostedServicesTask = tcs.Task;

        HostingEnvironment.QueueBackgroundWorkItem(async token =>
        {
            try
            {
                await host.StartAsync(token);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            finally
            {
                HostedServicesTask = Task.CompletedTask;
                tcs.SetResult(true);
            }
        });
    }

    private void StopHost()
    {
        IHost? host;
        lock (InitLock)
        {
            if (Host == null)
                return;

            host = Host;
            Host = null;
            IsInitialized = false;
            Delegate = null;
            HostedServicesTask = Task.CompletedTask;
        }

        try
        {
            Task.Run(async () =>
            {
                try
                {
                    await host.StopAsync(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                switch (host)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
        }
    }
}
