using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS.Consensus.DPos
{
    
    [DependsOn(
        typeof(OSAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class OSConsensusDPosTestModule_BP : AElfModule
    {
        private readonly ECKeyPair _keyPair = CryptoHelper.FromPrivateKey(
            ByteArrayHelper.HexStringToByteArray(OSConsensusDPosTestConstants.PrivateKeyHex));
        
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<IAccountService, AccountService>();
            services.AddSingleton<IPeerPool, PeerPool>();

            services.AddTransient(o =>
            {
                var mockService = new Mock<IAEDPoSInformationProvider>();
                mockService.Setup(m=>m.GetCurrentMinerList(It.IsAny<ChainContext>()))
                    .Returns(async ()=>
                        await Task.FromResult(new []{
                            OSConsensusDPosTestConstants.Bp1PublicKey,
                            OSConsensusDPosTestConstants.Bp2PublicKey,
                            _keyPair.PublicKey.ToHex()
                        }));
                return mockService.Object;

            });
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            context.ServiceProvider.GetService<IAElfAsymmetricCipherKeyPairProvider>()
                .SetKeyPair(_keyPair);
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var peerPool = context.ServiceProvider.GetRequiredService<IKnownBlockCacheProvider>();
            var osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();
            var blocks = osTestHelper.BestBranchBlockList.GetRange(0, 6);
            foreach (var block in blocks)
            {
                peerPool.AddKnownBlock(block.Height,block.GetHash(),false);
            }
        }
    }
}