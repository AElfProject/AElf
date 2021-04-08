using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.OS;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Network;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Benchmark
{
    [DependsOn(
        typeof(AEDPoSAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(KernelAElfModule),
        typeof(CoreOSAElfModule)
    )]
    public class BenchmarkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseInMemoryDatabase());

            services.AddSingleton<IDefaultContractZeroCodeProvider, DefaultContractZeroCodeProvider>();
            context.Services.AddSingleton<BenchmarkHelper>();
            
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
            });
            
            Configure<ChainOptions>(o => { o.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });

            context.Services.AddTransient(o => Mock.Of<IConsensusService>());
            
            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).ToBase58();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelper.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });

            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());

            context.Services.AddTransient<ISystemTransactionGenerationService>(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(s =>
                        s.GenerateSystemTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(new List<Transaction>()));
                return mockService.Object;
            });

            context.Services.AddTransient<IBlockExtraDataService>(o =>
            {
                var mockService = new Mock<IBlockExtraDataService>();
                mockService.Setup(s =>
                    s.FillBlockExtraDataAsync(It.IsAny<BlockHeader>())).Returns(Task.CompletedTask);
                return mockService.Object;
            });

            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            context.Services.AddSingleton<ITxHub, MockTxHub>();

            context.Services.AddSingleton<ISmartContractRunner, UnitTestCSharpSmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new UnitTestCSharpSmartContractRunner(
                    option.Value.SdkDir);
            });
            context.Services.AddSingleton<ISmartContractAddressService, UnitTestSmartContractAddressService>();
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var osTestHelper = context.ServiceProvider.GetService<BenchmarkHelper>();
            AsyncHelper.RunSync(() => osTestHelper.MockChainAsync());
        }
    }

    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(KernelAElfModule),
        typeof(CSharpRuntimeAElfModule)
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
            
            context.Services.AddSingleton<BenchmarkHelper>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var keyPairProvider = context.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
            keyPairProvider.SetKeyPair(CryptoHelper.GenerateKeyPair());
        }
    }

    [DependsOn(
        typeof(BenchmarkAElfModule),
        typeof(ParallelExecutionModule)
    )]
    public class BenchmarkParallelAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}