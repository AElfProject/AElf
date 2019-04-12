using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network.Application
{
    public interface INetworkService
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<string> GetPeerIpList();
        Task<Block> GetBlockByHashAsync(Hash hash, string peer = null, bool tryOthersIfFail = false);
        Task<int> BroadcastAnnounceAsync(BlockHeader blockHeader);
        Task<int> BroadcastTransactionAsync(Transaction tx);
        Task<List<Block>> GetBlocksAsync(Hash previousBlock, long previousHeight, int count, string peerPubKey = null, bool tryOthersIfFail = false);
        Task<long> GetBestChainHeightAsync(string peerPubKey = null);
        
    }
}