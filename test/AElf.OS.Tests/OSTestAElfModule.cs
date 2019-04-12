using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Tests;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class OSTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelpers.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            context.Services.AddSingleton<IKeyStore>(o =>
            {
                var keyStore = new Mock<IKeyStore>();
                ECKeyPair keyPair = null;

                keyStore.Setup(k => k.GetAccountKeyPair(It.IsAny<string>())).Returns(() => keyPair);

                keyStore.Setup(k => k.OpenAsync(It.IsAny<string>(), It.IsAny<string>(), false)).Returns(() =>
                {
                    keyPair = ecKeyPair;
                    return Task.FromResult(AElfKeyStore.Errors.None);
                });

                return keyStore.Object;
            });

            context.Services.AddTransient<AccountService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {

        }
    }
}