using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network
{
    public interface INetworkService
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<string> GetPeers();
        
        Task<IBlock> GetBlockByHashAsync(Hash hash, string peer = null, bool tryOthersIfFail = false);
        Task<List<Hash>> GetBlockIdsAsync(Hash topHash, int count, string peer);
        Task BroadcastAnnounceAsync(BlockHeader blockHeader);
        Task BroadcastTransactionAsync(Transaction tx);
    }
}