using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network
{
    public interface INetworkManager
    {
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerAsync(string address);
        List<string> GetPeers();
        
        Task<IBlock> GetBlockByHash(Hash hash, string peer = null);
        Task BroadcastAnnounce(Block b);
        
        Task StartAsync();
        Task StopAsync();
    }
}