using System.Collections.Generic;
using System.Threading.Tasks;

using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using AElf.OS.Network.Infrastructure;
using AElf.Types;

namespace AElf.OS.Network.Application
{
    public interface INetworkService
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<string> GetPeerIpList();
        List<IPeer> GetPeers();
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash, string peer = null, bool tryOthersIfFail = false);
        Task<int> BroadcastAnnounceAsync(BlockAcceptedEvent blockAcceptedEvent);
        Task<int> BroadcastTransactionAsync(Transaction tx);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousBlock, long previousHeight, int count, string peerPubKey = null, bool tryOthersIfFail = false);
        Task<long> GetBestChainHeightAsync(string peerPubKey = null);
        
    }
}