using System.Xml.Schema;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Grains;
using AElf.Kernel.SmartContract.Orleans;
using Microsoft.Extensions.Configuration;
//using AutoMapper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp.Auditing;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.ObjectMapping;

namespace AElf.Kernel.SmartContract.Orleans;

[DependsOn(
    // typeof(AbpAutoMapperModule)
    )]
public class SiloExecutionAElfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        ConfigureOrleans(context, configuration);
        
        context.Services.AddSingleton<IPlainTransactionExecutingService, SiloTransactionExecutingService>();
        context.Services.AddSingleton<IPlainTransactionExecutingGrain, PlainTransactionExecutingGrain>();
        context.Services.AddSingleton<ISiloClusterClientContext, SiloClusterClientContext>();

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
                    ;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                /*.ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(CAServerGrainsModule).Assembly).WithReferences())*/
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }
}