using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network
{
    public interface INetworkService
    {
        Task AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<string> GetPeers();

        Task BroadcastTransaction(Transaction tx);
        
        Task<IBlock> GetBlockByHash(Hash hash, string peer);
        Task BroadcastAnnounce(Hash b);
    }
}