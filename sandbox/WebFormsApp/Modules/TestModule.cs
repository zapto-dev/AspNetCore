using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Zapto.AspNetCore;
using Zapto.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace WebFormsApp.Modules
{
    public class TestModule : AspNetCoreModule
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddLogging();
            services.AddMvcCore().AddApplicationPart(typeof(TestModule).Assembly);

            services.AddSingleton<CounterService>();
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseMvc();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/ws/echo") && context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var buffer = new byte[1024 * 4];

                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), context.RequestAborted);
                        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            break;
                        }

                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                    }

                    return;
                }

                await next();
            });
        }
    }
}
