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
        List<IPeer> GetPeers();
        Task<Response<BlockWithTransactions>> GetBlockByHashAsync(Hash hash, string peer);
        Task<Response<List<BlockWithTransactions>>> GetBlocksAsync(Hash previousBlock, int count, string peer);
        Task BroadcastAnnounceAsync(BlockHeader blockHeader, bool hasFork);
        Task BroadcastTransactionAsync(Transaction transaction);
        Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions);
    }
}