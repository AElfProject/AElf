using System;
using System.Threading.Tasks;
using AElf.Network.Data;
using System.Threading.Tasks;
using AElf.Network;
using AElf.Network.Peers;
using AElf.Node.Protocol;
using AElf.RPC;
using Community.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;

namespace AElf.Net.Rpc
{
    [Path("/net")]
    public class NetRpcService : IJsonRpcService
    {
        public IPeerManager Manager { get; set; }
        public IBlockSynchronizer BlockSynchronizer { get; set; }
        public INetworkManager NetworkManager { get; set; }

        [JsonRpcMethod("get_peers")]
        public async Task<JObject> GetPeers()
        {
            return await Manager.GetPeers();
        }

        [JsonRpcMethod("add_peer", "address")]
        public async Task<JObject> AddPeer(string address)
        {
            NodeData nodeData = null;
            
            try
            {
                nodeData = NodeData.FromString(address);
            }
            catch { }

            if (nodeData == null)
            {
                throw new JsonRpcServiceException(-32602, "Invalid address");
            }

            await Task.Run(() => Manager.AddPeer(nodeData));
            
            return new JObject { ["result"] = true };
        }
        
        [JsonRpcMethod("remove_peer", "address")]
        public async Task<JObject> RemovePeer(string address)
        {
            NodeData nodeData = null;
            
            try
            {
                nodeData = NodeData.FromString(address);
            }
            catch { }

            if (nodeData == null)
            {
                throw new JsonRpcServiceException(-32602, "Invalid address");
            }

            await Task.Run(() => Manager.RemovePeer(nodeData));
            
            return new JObject { ["result"] = true };
        }

        [JsonRpcMethod("get_pool_state")]
        public async Task<JObject> GetPoolState()
        {
            var pendingRequestCount = NetworkManager.GetPendingRequestCount();
            var jobQueueCount = BlockSynchronizer.GetJobQueueCount();
            
            var response = new JObject
            {
                ["RequestPoolSize"] = pendingRequestCount,
                ["ReceivePoolSize"] = jobQueueCount
            };

            return JObject.FromObject(response);
        }
    }
}