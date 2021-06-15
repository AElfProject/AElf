using System;
using System.Net;
using AElf.Contracts.Genesis;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel.Orleans;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ExecutionWorker
{
    [DependsOn(
        typeof(CoreKernelAElfModule),
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
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var advertisedIPAddress = configuration.GetSection("Orleans:Endpoint:AdvertisedIPAddress").Value;
            var siloPort = configuration.GetSection("Orleans:Endpoint:SiloPort").Value;
            var gatewayPort = configuration.GetSection("Orleans:Endpoint:GatewayPort").Value;
            Configure<EndpointOptions>(o =>
            {
                o.AdvertisedIPAddress = IPAddress.Parse(advertisedIPAddress);
                o.SiloPort = Convert.ToInt32(siloPort);
                o.GatewayPort = Convert.ToInt32(gatewayPort);
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractZeroCodeProvider =
                context.ServiceProvider.GetRequiredService<IDefaultContractZeroCodeProvider>();
            contractZeroCodeProvider.SetDefaultContractZeroRegistrationByType(typeof(BasicContractZero));
        }
    }
}