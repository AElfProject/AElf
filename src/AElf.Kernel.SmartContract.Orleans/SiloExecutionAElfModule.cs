using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Runtime;
using Orleans.Runtime.Placement;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Orleans;

public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureOrleans(context, configuration); 
        context.Services.AddSingleton<IPlainTransactionExecutingService, SiloTransactionExecutingService>();
        context.Services.AddSingleton<ISiloClusterClientContext, SiloClusterClientContext>();
        context.Services.AddSingletonNamedService<PlacementStrategy, CleanCacheStrategy>(nameof(CleanCacheStrategy));
        context.Services.AddSingletonKeyedService<Type, IPlacementDirector, CleanCacheStrategyFixedSiloDirector>(
            typeof(CleanCacheStrategy));
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        StopOrleans(context.ServiceProvider);
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .Configure<GatewayOptions>(options =>
                {
                    options.PreferedGatewayIndex = ClusterClientConstants.PreferedGatewayIndex;
                    options.GatewayListRefreshPeriod = TimeSpan.FromSeconds(ClusterClientConstants.GatewayListRefreshPeriod);
                })
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Configure<ClientMessagingOptions>(opt =>
                {
                    opt.ResponseTimeout = TimeSpan.FromSeconds(ClusterClientConstants.ResponseTimeout);
                })
                .Build();
        });
    }

    private static void StartOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        if(client.IsInitialized)
            return;
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    private static void StopOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}