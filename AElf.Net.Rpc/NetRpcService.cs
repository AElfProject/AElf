using System.Threading.Tasks;
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
    }
}