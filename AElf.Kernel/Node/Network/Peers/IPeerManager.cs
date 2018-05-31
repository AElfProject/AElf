using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerManager
    {
        void Start();
        Task AddPeer(IPeer peer);
        Task AddPeer(NodeData nodeData);
        List<NodeData> GetPeers(ushort numPeers);

        Task<bool> BroadcastMessage(MessageTypes messageType, byte[] payload);
        
        void SetCommandContext(MainChainNode node);
    }
}