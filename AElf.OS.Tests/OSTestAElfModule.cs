using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Consensus.DPoS.Tests;
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
        typeof(OSCoreTestAElfModule)
    )]
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
                Mock.Of<IKeyStore>(
                    c => c.OpenAsync(nodeAccount, nodeAccountPassword, false) ==
                         Task.FromResult(AElfKeyStore.Errors.None) &&
                         c.GetAccountKeyPair(nodeAccount) == ecKeyPair)
            );

            context.Services.AddTransient<AccountService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {

        }
    }
}