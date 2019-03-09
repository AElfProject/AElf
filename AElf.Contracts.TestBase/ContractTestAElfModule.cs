using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule),
        typeof(KernelTestAElfModule)
    )]
    public class ContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
            services.AddSingleton(o => Mock.Of<IPeerPool>());

            services.AddSingleton(o => Mock.Of<INetworkService>());
            
            // When testing contract and packaging transactions, no need to generate and schedule real consensus stuff.
            context.Services.AddSingleton(o => Mock.Of<IConsensusInformationGenerationService>());
            context.Services.AddSingleton(o => Mock.Of<IConsensusScheduler>());
            
            Configure<ChainOptions>(o => { o.ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            context.Services.AddSingleton(o =>
                Mock.Of<IKeyStore>(
                    c => c.OpenAsync(nodeAccount, nodeAccountPassword, false) ==
                         Task.FromResult(AElfKeyStore.Errors.None) &&
                         c.GetAccountKeyPair(nodeAccount) == ecKeyPair)
            );
            
            Configure<DPoSOptions>(o =>
            {
                var miners = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    miners.Add(CryptoHelpers.GenerateKeyPair().PublicKey.ToHex());
                }

                o.InitialMiners = miners;
                o.MiningInterval = 4000;
                o.IsBootMiner = true;
            });

            //services.AddSingleton<ILocalEventBus, NullLocalEventBus>();
        }
    }
}