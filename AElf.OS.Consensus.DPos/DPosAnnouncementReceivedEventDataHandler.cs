using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Consensus.DPos
{
    public class DPosAnnouncementReceivedEventDataHandler : ILocalEventHandler<AnnouncementReceivedEventData>
    {
        public DPosAnnouncementReceivedEventDataHandler()
        {
        }

        public async Task HandleEventAsync(AnnouncementReceivedEventData eventData)
        {
            throw new System.NotImplementedException();
        }

    }


    public interface IDPosLastLastIrreversibleBlockDiscoveryService
    {
        Hash FindLastLastIrreversibleBlockHash(string senderPubKey);
    }
    
    public class DPosLastLastIrreversibleBlockDiscoveryService : IDPosLastLastIrreversibleBlockDiscoveryService, ISingletonDependency
    {
        private readonly IPeerPool _peerPool;
        
        

        public DPosLastLastIrreversibleBlockDiscoveryService(IPeerPool peerPool)
        {
            _peerPool = peerPool;
        }

        public Hash FindLastLastIrreversibleBlockHash(string senderPubKey)
        {
            var senderPeer = _peerPool.FindPeerByPublicKey(senderPubKey);
            
            var peers = _peerPool.GetPeers();

            if (peers.Count == 0)
                return null;

            var orderedBlocks = senderPeer.RecentBlockHeightAndHashMappings.OrderBy(p => p.Key).ToList();

            foreach (var block in orderedBlocks)
            {
                var a = peers.Where(p =>
                {
                    p.RecentBlockHeightAndHashMappings.TryGetValue(block.Key, out var hash);
                    return hash == block.Value;
                });
            }
            
            var peerGroup = peers.GroupBy(p => p.CurrentBlockHash).Select(p => new
            {
                Count = p.Count(),
                Hash = p.First().CurrentBlockHash,
                Height = p.First().CurrentBlockHeight
            }).OrderByDescending(p => p.Count).First();

            //TODO: get params from DPOS consensus service 
            if (peerGroup.Count > 13)
            {
                
            }

            return null;
        }
    }
}