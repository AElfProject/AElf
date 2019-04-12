using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSTestBaseAElfModule)
    )]
    public class OSCoreTestAElfModule : AElfModule
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

            context.Services.AddTransient<IAccountService>(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                {
                    var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                    return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                });

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });
        }
    }
}