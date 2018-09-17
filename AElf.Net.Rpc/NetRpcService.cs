using System;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers;
using AElf.RPC;
using Community.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;

namespace AElf.Net.Rpc
{
    [Path("/net")]
    public class NetRpcService : IJsonRpcService
    {
        public IPeerManager Manager { get; set; }
        
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
    }
}