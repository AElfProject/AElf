using System;
using System.Threading.Tasks;
using AElf.Network.Data;
using Newtonsoft.Json.Linq;

namespace AElf.Network.Peers
{
    public interface IPeerManager : IDisposable
    {
        //todo: should remove event
        event EventHandler PeerEvent;
        
        void Start();
        Task Stop();
        
        // RPC methods
        Task<JObject> GetPeers();
        void AddPeer(NodeData address);
        void RemovePeer(NodeData address);
    }
}