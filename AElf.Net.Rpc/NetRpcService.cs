using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network;
using AElf.Network.Peers;
using AElf.RPC;
using Anemonis.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;

namespace AElf.Net.Rpc
{
    [Path("/net")]
    public class NetRpcService : IJsonRpcService
    {
        public IPeerManager Manager { get; set; }
        //public IBlockSynchronizer BlockSynchronizer { get; set; }
        public INetworkManager NetworkManager { get; set; }

        [JsonRpcMethod("GetPeers")]
        public async Task<JObject> GetPeers()
        {
            return await Manager.GetPeers();
        }

        [JsonRpcMethod("AddPeer", "address")]
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
                throw new JsonRpcServiceException(NetRpcErrorConsts.InvalidNetworkAddress,
                    NetRpcErrorConsts.RpcErrorMessage[NetRpcErrorConsts.InvalidNetworkAddress]);
            }

            await Task.Run(() => Manager.AddPeer(nodeData));
            
            return new JObject { true };
        }
        
        [JsonRpcMethod("RemovePeer", "address")]
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
                throw new JsonRpcServiceException(NetRpcErrorConsts.InvalidNetworkAddress,
                    NetRpcErrorConsts.RpcErrorMessage[NetRpcErrorConsts.InvalidNetworkAddress]);
            }

            await Task.Run(() => Manager.RemovePeer(nodeData));
            
            return new JObject { true };
        }

        //TODO:
/*        [JsonRpcMethod("get_pool_state")]
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
        }*/
    }
}