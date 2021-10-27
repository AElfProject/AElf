using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public class PeerInvalidTransactionProcessingService : IPeerInvalidTransactionProcessingService,
        ITransientDependency
    {
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;
        private readonly IPeerInvalidTransactionProvider _peerInvalidTransactionProvider;

        public PeerInvalidTransactionProcessingService(INetworkService networkService, IPeerPool peerPool,
            IPeerInvalidTransactionProvider peerInvalidTransactionProvider)
        {
            _networkService = networkService;
            _peerPool = peerPool;
            _peerInvalidTransactionProvider = peerInvalidTransactionProvider;
        }

        public async Task ProcessPeerInvalidTransactionAsync(Hash transactionId)
        {
            var knowsTransactionPeers = _peerPool.GetPeers().Where(p => p.KnowsTransaction(transactionId)).ToList();
            var toRemovePeerPubkey = new List<string>();

            foreach (var knowsTransactionPeer in knowsTransactionPeers)
            {
                var host = knowsTransactionPeer.RemoteEndpoint.Host;
                if (_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, transactionId))
                    continue;

                var peers = _peerPool.GetPeersByHost(host);
                toRemovePeerPubkey.AddRange(peers.Select(p => p.Info.Pubkey));

                _peerInvalidTransactionProvider.TryRemoveInvalidRecord(host);
            }

            foreach (var pubkey in toRemovePeerPubkey)
            {
                await _networkService.RemovePeerByPubkeyAsync(pubkey);
            }
        }
    }
}