using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.HostedServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Byndyusoft.Net.RabbitMq
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureLogging(i => i.AddConsole());
                    //webBuilder.UseSerilog((context, configuration) => configuration
                    //    .UseDefaultSettings(context.Configuration)
                    //    .UseOpenTelemetryTraces()
                    //    .WriteToOpenTelemetry(activityEventBuilder: StructuredActivityEventBuilder.Instance));
                });
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenTelemetry()
                        .WithTracing(builder => builder
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Byndyusoft.Net.RabbitMq"))
                            .AddJaegerExporter(jaeger =>
                            {
                                jaeger.AgentHost = "localhost";
                                jaeger.AgentPort = 6831;
                            })
                            .AddRabbitMqClientInstrumentation(o =>
                            {
                                o.LogEventsInTrace = true;
                                o.LogEventsInLogs = true;
                                o.RecordExceptions = true;
                            }));
            services.AddRabbitMqRpc();
            services.AddSingleton<MathRpcServiceClient>();
            services.AddRpcService<MathRpcService>();

            //services.AddHostedService<PullingExample>();
            //services.AddHostedService<RetryAndErrorExample>();
            //services.AddHostedService<RpcExample>();

            services.AddHostedService<SubscribeAsMessagePackExample>();

            //services.AddHostedService<RpcServerExample>();
            //services.AddHostedService<SubscribeAsExample>();
            //services.AddHostedService<SubscribeAsJsonExample>();
            //services.AddHostedService<SubscribeExchangeExample>();
            //services.AddHostedService<ClientFactoryExample>();

            //services.AddHostedService<QueueInstallerHostedService>();

            services.AddRabbitMqClient("host=localhost;username=guest;password=guest");

            //services.AddRabbitMqClient("client-factory", "host=localhost;username=guest;password=guest");
            //services.AddInMemoryRabbitMqClient();

            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}