using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Runtime.CSharp;
using AElf.Runtime.CSharp.ExecutiveTokenPlugin;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Contracts.TestBase
{
    [Obsolete("Deprecated. Use AElf.Contracts.TestKit for contract testing.")]
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
                o.StartTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }
    }
}