using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class AnnouncementReceivedEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private readonly IPeerPool _peerPool;
        private readonly IAEDPoSInformationProvider _dpoSInformationProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;
        private readonly INetworkService _networkService;

        public AnnouncementReceivedEventHandler(IPeerPool peerPool,
            IAEDPoSInformationProvider dpoSInformationProvider, 
            IBlockchainService blockchainService,
            IAccountService accountService,
            INetworkService networkService)
        {
            _peerPool = peerPool;
            _dpoSInformationProvider = dpoSInformationProvider;
            _blockchainService = blockchainService;
            _accountService = accountService;
            _networkService = networkService;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var senderPeer = _peerPool.FindPeerByPublicKey(eventData.SenderPubKey);

            if (senderPeer == null)
                return;

            var orderedBlocks = senderPeer.RecentBlockHeightAndHashMappings.OrderByDescending(p => p.Key).ToList();
            if (orderedBlocks.Count == 0) return;

            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var pubkeyList = (await _dpoSInformationProvider.GetCurrentMinerList(chainContext)).ToList();

            var peers = _peerPool.GetPeers().Where(p => pubkeyList.Contains(p.PubKey)).ToList();

            var pubKey = (await _accountService.GetPublicKeyAsync()).ToHex();
            if (peers.Count == 0 && !pubkeyList.Contains(pubKey))
                return;

            foreach (var block in orderedBlocks)
            {
                var hasBlock = _peerPool.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var blockHash) &&
                               blockHash == block.Value;
                if (!hasBlock) continue;
                
                var peersHadBlockAmount = peers.Where(p =>
                {
                    p.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var hash);
                    return hash == block.Value;
                }).Count();
                if (pubkeyList.Contains(pubKey))
                    peersHadBlockAmount++;

                var sureAmount = pubkeyList.Count.Mul(2).Div(3) + 1;
                if (peersHadBlockAmount < sureAmount) continue;
                var _ = _networkService.BroadcastPreLibAnnounceAsync(block.Key, block.Value);
                return;
            }
        }
    }
}