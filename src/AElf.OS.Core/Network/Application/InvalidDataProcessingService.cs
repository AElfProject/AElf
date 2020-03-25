using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Application
{
    public class InvalidDataProcessingService : IInvalidDataProcessingService, ITransientDependency
    {
        private readonly INetworkService _networkService;
        private readonly IPeerPool _peerPool;
        private readonly IPeerInvalidDataProvider _peerInvalidDataProvider;

        public InvalidDataProcessingService(INetworkService networkService, IPeerPool peerPool,
            IPeerInvalidDataProvider peerInvalidDataProvider)
        {
            _networkService = networkService;
            _peerPool = peerPool;
            _peerInvalidDataProvider = peerInvalidDataProvider;
        }

        public async Task ProcessInvalidTransactionAsync(Hash transactionId)
        {
            var knowsTransactionPeers = _peerPool.GetPeers().Where(p => p.KnowsTransaction(transactionId)).ToList();
            var toRemovePeerPubkey = new List<string>();

            foreach (var knowsTransactionPeer in knowsTransactionPeers)
            {
                var host = knowsTransactionPeer.RemoteEndpoint.Host;
                if (_peerInvalidDataProvider.TryMarkInvalidData(host))
                    continue;

                var peers = _peerPool.GetPeersByHost(host);
                toRemovePeerPubkey.AddRange(peers.Select(p => p.Info.Pubkey));

                _peerInvalidDataProvider.TryRemoveInvalidData(host);
            }

            foreach (var pubkey in toRemovePeerPubkey)
            {
                await _networkService.RemovePeerByPubkeyAsync(pubkey, true);
            }
        }
    }
}