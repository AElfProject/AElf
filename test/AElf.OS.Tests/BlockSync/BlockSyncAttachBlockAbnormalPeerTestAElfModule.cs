using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using AElf.OS.Network.Application;
using AElf.OS.Network.Types;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.BlockSync
{
    [DependsOn(typeof(BlockSyncTestBaseAElfModule))]
    public class BlockSyncAttachBlockAbnormalPeerTestAElfModule : AElfModule
    {
        private readonly Dictionary<string, PeerInfo> _peers = new Dictionary<string, PeerInfo>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            _peers.Add("AbnormalPeerPubkey", new PeerInfo());
            
            context.Services.AddSingleton(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                
                networkServiceMock.Setup(p => p.RemovePeerByPubkeyAsync(It.IsAny<string>(),It.IsAny<int>()))
                    .Returns<string, int>(
                        (peerPubkey, removalSeconds) =>
                        {
                            _peers.Remove(peerPubkey);
                            return Task.FromResult(true);
                        });

                networkServiceMock.Setup(p => p.GetPeerByPubkey(It.IsAny<string>()))
                    .Returns<string>((peerPubKey) => _peers.ContainsKey(peerPubKey) ? _peers[peerPubKey] : null);

                return networkServiceMock.Object;
            });

            context.Services.AddTransient<IBlockValidationService>(o =>
            {
                var blockValidationServiceMock = new Mock<IBlockValidationService>();

                blockValidationServiceMock.Setup(p => p.ValidateBlockBeforeAttachAsync(It.IsAny<Block>()))
                    .Returns<Block>(block =>
                    {
                        if (block.Header.PreviousBlockHash.Equals(HashHelper.ComputeFrom("BadBlock")))
                        {
                            return Task.FromResult(false);
                        }

                        return Task.FromResult(true);
                    });
                blockValidationServiceMock.Setup(s =>
                    s.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));
                blockValidationServiceMock.Setup(s =>
                    s.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>())).Returns(Task.FromResult(true));

                return blockValidationServiceMock.Object;
            });
        }
    }
}