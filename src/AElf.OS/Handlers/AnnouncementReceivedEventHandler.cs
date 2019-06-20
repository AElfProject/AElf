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
            var blockHeight = eventData.Announce.BlockHeight;
            var blockHash = eventData.Announce.BlockHash;
            var hasBlock = _peerPool.RecentBlockHeightAndHashMappings.TryGetValue(blockHeight, out var hash) &&
                           hash == blockHash;
            if (!hasBlock) return;
            
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var pubkeyList = (await _dpoSInformationProvider.GetCurrentMinerList(chainContext)).ToList();

            var peers = _peerPool.GetPeers().Where(p => pubkeyList.Contains(p.PubKey)).ToList();

            var pubKey = (await _accountService.GetPublicKeyAsync()).ToHex();
            if (peers.Count == 0 && !pubkeyList.Contains(pubKey))
                return;

            var peersHadBlockAmount = peers.Count(p =>
                p.RecentBlockHeightAndHashMappings.TryGetValue(blockHeight, out hash) && hash == blockHash);
            if (pubkeyList.Contains(pubKey))
                peersHadBlockAmount++;

            var sureAmount = pubkeyList.Count.Mul(2).Div(3) + 1;
            if (peersHadBlockAmount < sureAmount) return;
            var peersHadPreLibAmount = peers.Count(p =>
                p.PreLibBlockHeightAndHashMappings.TryGetValue(blockHeight, out var preLibBlockInfo) &&
                preLibBlockInfo.BlockHash == blockHash);
            if (pubkeyList.Contains(pubKey))
                peersHadPreLibAmount++;
            var _ = _networkService.BroadcastPreLibAnnounceAsync(blockHeight, blockHash, peersHadPreLibAmount);
            var preLibBlocks = _peerPool.PreLibBlockHeightAndHashMappings.OrderByDescending(p => p.Key);
            foreach (var preLibBlock in preLibBlocks)
            {
                if (!_peerPool.RecentBlockHeightAndHashMappings.TryGetValue(preLibBlock.Key, out var preLibHash) ||
                    preLibHash != preLibBlock.Value.BlockHash)
                    continue;
                peersHadPreLibAmount = peers.Count(p =>
                    p.PreLibBlockHeightAndHashMappings.TryGetValue(preLibBlock.Key, out var preLibBlockInfo) &&
                    preLibBlockInfo.BlockHash == preLibHash);
                peersHadPreLibAmount++;
                if(peersHadPreLibAmount < sureAmount)
                    continue;
                _ = _networkService.BroadcastPreLibAnnounceAsync(preLibBlock.Key, preLibHash, peersHadPreLibAmount);
                break;
            }
        }
    }
}