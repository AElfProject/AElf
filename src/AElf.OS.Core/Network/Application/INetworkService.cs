using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Types;
using AElf.Types;

namespace AElf.OS.Network.Application
{
    public interface INetworkService
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<PeerInfo> GetPeers();
        PeerInfo GetPeerByPubkey(string peerPubkey);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash, string peerPubkey = null);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousBlock, int count, string peerPubkey = null);
        Task BroadcastAnnounceAsync(BlockHeader blockHeader, bool hasFork);
        Task BroadcastTransactionAsync(Transaction transaction);
        Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions);
        Task BroadcastLibAnnounceAsync(Hash libHash, long libHeight);
    }
}