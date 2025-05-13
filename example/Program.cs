using System.Diagnostics;
using System.Threading.Tasks;
using Byndyusoft.Net.RabbitMq.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Byndyusoft.Net.RabbitMq
{
    public static class Program
    {
        private static readonly ActivitySource ActivitySource = new(nameof(Program));

        public static async Task Main(string[] args)
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("Byndyusoft.Net.RabbitMq"))
                .SetSampler(new AlwaysOnSampler())
                .AddSource(ActivitySource.Name)
                .AddJaegerExporter(jaeger =>
                {
                    jaeger.AgentHost = "localhost";
                    jaeger.AgentPort = 6831;
                })
                .AddRabbitMqClientInstrumentation()
                .Build();

            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostOptions(options =>
                {
                    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                })
                .ConfigureLogging(log => log.AddConsole())
                .ConfigureAppConfiguration(configuration => { configuration.AddJsonFile("appsettings.json", true); })
                .ConfigureServices((_, services) =>
                {
                    //services.AddRabbitMqRpc();
                    //services.AddSingleton<MathRpcServiceClient>();
                    //services.AddRpcService<MathRpcService>();

                    //services.AddHostedService<PullingExample>();
                    //services.AddHostedService<RetryAndErrorExample>();
                    //services.AddHostedService<RpcExample>();

                    //services.AddHostedService<SubscribeAsMessagePackExample>();

                    //services.AddHostedService<RpcServerExample>();
                    //services.AddHostedService<SubscribeAsExample>();
                    services.AddHostedService<SubscribeAsJsonExample>();
                    //services.AddHostedService<SubscribeExchangeExample>();
                    //services.AddHostedService<ClientFactoryExample>();

                    //services.AddHostedService<QueueInstallerHostedService>();

                    //services.AddRabbitMqClient("host=localhost;username=guest;password=guest");

                    //services.AddRabbitMqClient("client-factory", "host=localhost;username=guest;password=guest");
                    services.AddInMemoryRabbitMqClient();

                    services.BuildServiceProvider(new ServiceProviderOptions
                    {
                        ValidateOnBuild = true,
                        ValidateScopes = true
                    });
                });
        }
    }
}