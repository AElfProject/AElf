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