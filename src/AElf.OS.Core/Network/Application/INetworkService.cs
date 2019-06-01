using System.Collections.Generic;
using System.Threading.Tasks;

using AElf.Kernel;
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
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash, string peer = null);
        Task<int> BroadcastAnnounceAsync(BlockHeader blockHeader, bool hasFork);
        Task<int> BroadcastTransactionAsync(Transaction tx);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousBlock, int count, string peerPubKey = null);
        Task<long> GetBestChainHeightAsync(string peerPubKey = null);
        
    }
}