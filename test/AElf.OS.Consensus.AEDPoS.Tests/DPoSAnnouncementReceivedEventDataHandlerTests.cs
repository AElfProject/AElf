using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Xunit;

namespace AElf.OS.Consensus.DPos
{
    public class DPoSAnnouncementReceivedEventDataHandlerTests : OSConsensusDPosTestBase
    {
        private DPoSAnnouncementReceivedEventDataHandler _dpoSAnnouncementReceivedEventDataHandler;
        private IBlockchainService _blockchainService;
        private IPeerPool _peerPool;
        
        public DPoSAnnouncementReceivedEventDataHandlerTests()
        {
            _dpoSAnnouncementReceivedEventDataHandler = GetRequiredService<DPoSAnnouncementReceivedEventDataHandler>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task HandleAnnounceReceiveEventAsync_IrreversibleBlockIndex_IsNull()
        {
            var an = new BlockAnnouncement { };
            var sendKey = string.Empty;
            var announcementData = new AnnouncementReceivedEventData(an, sendKey);

            await _dpoSAnnouncementReceivedEventDataHandler.HandleEventAsync(announcementData);
        }
        
        [Fact]
        public async Task HandleAnnounceReceiveEventAsync_IrreversibleBlockIndex_SureAmountNotEnough()
        {
            var block = await GenerateNewBlockAndAnnouncementToPeers(1);
            var an = new BlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            var sendKey = CryptoHelper.GenerateKeyPair().PublicKey.ToHex();
            var announcementData = new AnnouncementReceivedEventData(an, sendKey);

            await _dpoSAnnouncementReceivedEventDataHandler.HandleEventAsync(announcementData);
        }
        
        [Fact]
        public async Task HandleAnnounceReceiveEventAsync_IrreversibleBlockIndex_SureAmountEnough()
        {
            var block = await GenerateNewBlockAndAnnouncementToPeers(3);
            var an = new BlockAnnouncement
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            var sendKey = CryptoHelper.GenerateKeyPair().PublicKey.ToHex();
            var announcementData = new AnnouncementReceivedEventData(an, sendKey);

            await _dpoSAnnouncementReceivedEventDataHandler.HandleEventAsync(announcementData);
        }

        private async Task<Block> GenerateNewBlockAndAnnouncementToPeers(int number = 1)
        {
            var chain = await _blockchainService.GetChainAsync();
            var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, 6, chain.LongestChainHash);
            var block = await _blockchainService.GetBlockByHashAsync(hash);
            
            var peers = _peerPool.GetPeers();
            for(int i=0; i<number; i++)
            {
                var grpcPeer = peers[i] as GrpcPeer;
                grpcPeer.AddKnowBlock(new BlockAnnouncement
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });
            }

            return block;
        }
    }
}