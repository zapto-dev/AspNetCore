using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zapto.AspNetCore.Collections;
using Zapto.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Zapto.AspNetCore;

/// <summary>
/// Default implementation for <see cref="IApplicationBuilder"/>.
/// </summary>
internal sealed class ApplicationBuilder : IApplicationBuilder
{
    private const string ServerFeaturesKey = "server.Features";
    private const string ApplicationServicesKey = "application.Services";
    public const string RequestUnhandledKey = "__RequestUnhandled";


    private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
    public ApplicationBuilder(IServiceProvider serviceProvider) : this(serviceProvider, new FeatureCollection())
    {
    }
 
    private int MiddlewareCount => _components.Count;
 
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationBuilder"/>.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> for application services.</param>
    /// <param name="server">The server instance that hosts the application.</param>
    public ApplicationBuilder(IServiceProvider serviceProvider, object server)
    {
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        ApplicationServices = serviceProvider;
 
        SetProperty(ServerFeaturesKey, server);
    }
 
    private ApplicationBuilder(ApplicationBuilder builder)
    {
        Properties = new CopyOnWriteDictionary<string, object>(builder.Properties, StringComparer.Ordinal);
    }
 
    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for application services.
    /// </summary>
    public IServiceProvider ApplicationServices
    {
        get
        {
            return GetProperty<IServiceProvider>(ApplicationServicesKey)!;
        }
        set
        {
            SetProperty<IServiceProvider>(ApplicationServicesKey, value);
        }
    }
 
    /// <summary>
    /// Gets the <see cref="IFeatureCollection"/> for server features.
    /// </summary>
    /// <remarks>
    /// An empty collection is returned if a server wasn't specified for the application builder.
    /// </remarks>
    public IFeatureCollection ServerFeatures
    {
        get
        {
            return GetProperty<IFeatureCollection>(ServerFeaturesKey)!;
        }
    }
 
    /// <summary>
    /// Gets a set of properties for <see cref="ApplicationBuilder"/>.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }
 
    private T? GetProperty<T>(string key)
    {
        return Properties.TryGetValue(key, out var value) ? (T?)value : default(T);
    }
 
    private void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }
 
    /// <summary>
    /// Adds the middleware to the application request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware.</param>
    /// <returns>An instance of <see cref="IApplicationBuilder"/> after the operation has completed.</returns>
    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }
 
    /// <summary>
    /// Creates a copy of this application builder.
    /// <para>
    /// The created clone has the same properties as the current instance, but does not copy
    /// the request pipeline.
    /// </para>
    /// </summary>
    /// <returns>The cloned instance.</returns>
    public IApplicationBuilder New()
    {
        return new ApplicationBuilder(this);
    }
 
    /// <summary>
    /// Produces a <see cref="RequestDelegate"/> that executes added middlewares.
    /// </summary>
    /// <returns>The <see cref="RequestDelegate"/>.</returns>
    public RequestDelegate Build()
    {
        RequestDelegate app = async context =>
        {
            // Communicates to higher layers that the request wasn't handled by the app pipeline.
            context.Items[RequestUnhandledKey] = true;

            var contextImpl = context as HttpContextImpl ?? AspNetContext.Current?.Items[typeof(HttpContextImpl)] as HttpContextImpl;

            if (contextImpl == null)
            {
                throw new InvalidOperationException("The HttpContextImpl is not available.");
            }

            await contextImpl.FinishStackAsync();
        };
 
        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }
 
        return app;
    }
}