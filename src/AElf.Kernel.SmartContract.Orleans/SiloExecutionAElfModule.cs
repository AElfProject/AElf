using AElf.Kernel.SmartContract.Application;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.Configuration;
//using AutoMapper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(typeof(CSharpRuntimeAElfModule), typeof(SmartContractAElfModule))]
public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureOrleans(context, configuration); 
        context.Services.AddSingleton<IPlainTransactionExecutingService, SiloTransactionExecutingService>();
        context.Services.AddSingleton<IPlainTransactionExecutingGrain, PlainTransactionExecutingGrain>();
        context.Services.AddSingleton<ISiloClusterClientContext, SiloClusterClientContext>();
        context.Services.AddSingleton<ISmartContractExecutiveService, SmartContractExecutiveService>();
        context.Services.AddSingleton<IPlainTransactionExecutingGrainProvider, PlainTransactionExecutingGrainProvider>();

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
        var launcherType = configuration.GetValue("LaunchProgram", LaunchProgram.BP);
        if (launcherType != LaunchProgram.BP)
        {
            return;
        }
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
                    options.PreferedGatewayIndex = -1;
                    options.GatewayListRefreshPeriod = TimeSpan.FromSeconds(10);
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(SiloExecutionAElfModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Configure<PerformanceTuningOptions>(opt =>
                {
                    //opt.Expect100Continue = true;
                    //opt.UseNagleAlgorithm = true;
                    opt.MinDotNetThreadPoolSize = 20480;
                    opt.MinIOThreadPoolSize = 200;
                    opt.DefaultConnectionLimit = 200;
                })
                .Configure<SchedulingOptions>(opt =>
                {
                    opt.MaxActiveThreads = 200;
                })
                .Configure<ClientMessagingOptions>(opt =>
                {
                    opt.ResponseTimeout = TimeSpan.FromSeconds(30);
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