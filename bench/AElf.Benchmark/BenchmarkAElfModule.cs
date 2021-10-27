using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.OS;
using AElf.Kernel.SmartContract.Parallel;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class BenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }

    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(KernelAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(TestBaseKernelAElfModule)
    )]
    public class MiningBenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            context.Services.AddTransient<AccountService>();
            context.Services.AddTransient(o => Mock.Of<IConsensusService>());
            // Configure<TransactionOptions>(options => options.EnableTransactionExecutionValidation = false);
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var keyPairProvider = context.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
            keyPairProvider.SetKeyPair(CryptoHelper.GenerateKeyPair());
        }
    }

    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class BenchmarkParallelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}