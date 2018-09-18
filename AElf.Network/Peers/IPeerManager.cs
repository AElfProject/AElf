using System;
using System.Threading.Tasks;
using AElf.Network.Data;
using Newtonsoft.Json.Linq;

namespace AElf.Network.Peers
{
    public interface IPeerManager : IDisposable
    {
        event EventHandler PeerEvent;
        
        void Start();
        
        // RPC methods
        Task<JObject> GetPeers();
        void AddPeer(NodeData address);
        void RemovePeer(NodeData address);
    }
}