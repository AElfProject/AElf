using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSTestBaseAElfModule)
    )]
    public class OSCoreTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            Configure<ConsensusOptions>(o =>
            {
                var miners = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    miners.Add(CryptoHelper.GenerateKeyPair().PublicKey.ToHex());
                }

                o.InitialMiners = miners;
                o.MiningInterval = 4000;
                o.TimeEachTerm = 604800;
                o.MinerIncreaseInterval = 31536000;
            });

            context.Services.AddTransient<IAccountService>(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelper.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });

            context.Services.AddSingleton(o => Mock.Of<IAElfNetworkServer>());
        }
    }
}