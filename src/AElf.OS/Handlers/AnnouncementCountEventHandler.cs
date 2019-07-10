using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers
{
    public class AnnouncementCountEventHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private readonly IPeerPool _peerPool;
        private readonly IAEDPoSInformationProvider _dpoSInformationProvider;
        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;
        private readonly INetworkService _networkService;

        public AnnouncementCountEventHandler(IPeerPool peerPool,
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
            if (!_peerPool.HasBlock(blockHeight,blockHash)) return;
            
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight};
            var pubkeyList = (await _dpoSInformationProvider.GetCurrentMinerList(chainContext)).ToList();

            var peers = _peerPool.GetPeers().Where(p => pubkeyList.Contains(p.Info.Pubkey)).ToList();

            var pubKey = (await _accountService.GetPublicKeyAsync()).ToHex();
            if (peers.Count == 0 && !pubkeyList.Contains(pubKey))
                return;
                
            var peersHadBlockAmount = peers.Count(p => p.HasBlock(blockHeight, blockHash));
            if (pubkeyList.Contains(pubKey))
                peersHadBlockAmount++;

            var sureAmount = pubkeyList.Count;
            if (peersHadBlockAmount < sureAmount) return;
            var peersHadPreLibAmount =
                peers.Count(p => p.HasBlock(blockHeight, blockHash) && p.HasPreLib(blockHeight, blockHash));
            if (pubkeyList.Contains(pubKey))
                peersHadPreLibAmount++;
            var _ = _networkService.BroadcastPreLibAnnounceAsync(blockHeight, blockHash, peersHadPreLibAmount);
        }
    }
}