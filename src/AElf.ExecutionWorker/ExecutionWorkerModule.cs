using System.Net;
using AElf.Contracts.Genesis;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel.Orleans;
using AElf.Kernel.SmartContract.Parallel.Orleans.Application;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.ExecutionWorker
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(AEDPoSAElfModule),
        typeof(TokenKernelAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CSharpCodeOpsAElfModule),
        typeof(RuntimeSetupAElfModule),
        typeof(OrleansParallelExecutionCoreModule),
        //plugin
        typeof(ExecutionPluginForMethodFeeModule),
        typeof(ExecutionPluginForResourceFeeModule),
        typeof(ExecutionPluginForCallThresholdModule)
    )]
    public class ExecutionWorkerModule: AElfModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractZeroCodeProvider =
                context.ServiceProvider.GetRequiredService<IDefaultContractZeroCodeProvider>();
            contractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
            
            var silo = new SiloHostBuilder()
                .ConfigureDefaults()
                .UseLocalhostClustering(primarySiloEndpoint: new IPEndPoint(IPAddress.Loopback, 11111))
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "dev";
                })
                .Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                    options.SiloPort = 11111;
                    options.GatewayPort = 21111;
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(TransactionExecutingGrain).Assembly)
                        .WithReferences())
                .AddMemoryGrainStorage(name: "ArchiveStorage")
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                })
                .Build();

            AsyncHelper.RunSync(async () => await silo.StartAsync());
        }
    }
}