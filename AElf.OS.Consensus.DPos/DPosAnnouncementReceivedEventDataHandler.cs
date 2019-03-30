using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Consensus.DPos
{
    public class DPosAnnouncementReceivedEventDataHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        private ITaskQueueManager _taskQueueManager;
        private IDPosLastLastIrreversibleBlockDiscoveryService _dPosLastLastIrreversibleBlockDiscoveryService;

        public DPosAnnouncementReceivedEventDataHandler(ITaskQueueManager taskQueueManager,
            IDPosLastLastIrreversibleBlockDiscoveryService dPosLastLastIrreversibleBlockDiscoveryService)
        {
            _taskQueueManager = taskQueueManager;
            _dPosLastLastIrreversibleBlockDiscoveryService = dPosLastLastIrreversibleBlockDiscoveryService;
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            var hash = _dPosLastLastIrreversibleBlockDiscoveryService.FindLastLastIrreversibleBlockHash(
                eventData.SenderPubKey);

            if (hash != null)
            {
                _taskQueueManager.Enqueue(() =>
                {
                    
                    //TODO: should call set LIB
                    return Task.CompletedTask;
                });
            }
            
        }
    }


    public interface IDPosLastLastIrreversibleBlockDiscoveryService
    {
        Hash FindLastLastIrreversibleBlockHash(string senderPubKey);
    }

    public class DPosLastLastIrreversibleBlockDiscoveryService : IDPosLastLastIrreversibleBlockDiscoveryService,
        ISingletonDependency
    {
        private readonly IPeerPool _peerPool;

        public DPosLastLastIrreversibleBlockDiscoveryService(IPeerPool peerPool)
        {
            _peerPool = peerPool;
            //LocalEventBus = NullLocalEventBus.Instance;
        }

        public Hash FindLastLastIrreversibleBlockHash(string senderPubKey)
        {
            var senderPeer = _peerPool.FindPeerByPublicKey(senderPubKey);

            if (senderPeer == null)
                return null;

            var orderedBlocks = senderPeer.RecentBlockHeightAndHashMappings.OrderBy(p => p.Key).ToList();


            //TODO: should get from DPos Consensus service
            var pubkeyList = new string[] { };
            
            var peers = _peerPool.GetPeers().Where(p=>pubkeyList.Contains( p.PubKey )).ToList();

            if (peers.Count == 0)
                return null;

            foreach (var block in orderedBlocks)
            {
                var peersHadBlockAmount = peers.Where(p =>
                {
                    p.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var hash);
                    return hash == block.Value;
                }).Count();

                //TODO: get value from DPOS consensus service
                if (peersHadBlockAmount > 12)
                {
                    return block.Value;
                }
            }


            return null;
        }
    }
}