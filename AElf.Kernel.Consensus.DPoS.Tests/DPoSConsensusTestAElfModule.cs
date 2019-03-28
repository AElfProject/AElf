using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.OS;
using AElf.Runtime.CSharp;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS
{
    [DependsOn(
        typeof(KernelCoreTestAElfModule),
        typeof(DPoSConsensusAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreOSAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    public class DPoSConsensusTestAElfModule : TestBaseAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //Account service
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
            context.Services.AddTransient(o =>
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
            context.Services.AddTransient(builder =>
            {
                var consensusService = new Mock<IConsensusService>();
                consensusService.Setup(m => m.ValidateConsensusBeforeExecutionAsync(It.IsAny<Hash>(), It.IsAny<long>(),
                        It.IsAny<byte[]>()))
                    .Returns(Task.FromResult(true));
                consensusService.Setup(m => m.ValidateConsensusAfterExecutionAsync(It.IsAny<Hash>(), It.IsAny<long>(),
                        It.IsAny<byte[]>()))
                    .Returns(Task.FromResult(true));

                return consensusService.Object;
            });
            context.Services.AddTransient(o => Mock.Of<ConsensusControlInformation>());
            Configure<DPoSOptions>(o =>
            {
                o.InitialMiners = new List<string>
                {
                    ecKeyPair.PublicKey.ToHex()
                };
                o.InitialTermNumber = 1;
                o.MiningInterval = 2000;
                o.IsBootMiner = true;
            });
            context.Services.AddTransient<IBlockExtraDataProvider, ConsensusExtraDataProvider>();
            context.Services.AddTransient<IConsensusInformationGenerationService, DPoSInformationGenerationService>();
        }
    }
}