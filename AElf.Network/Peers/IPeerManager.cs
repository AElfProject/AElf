using System;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerManager
    {
        event EventHandler MessageReceived;
        
        void Start();
        void AddPeer(IPeer peer);

        Task<bool> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId);
        
        //void SetCommandContext(MainChainNode node);
    }
}