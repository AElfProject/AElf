using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;
using Newtonsoft.Json.Linq;
using NLog;

namespace AElf.Management.Services
{
    public class NetworkService:INetworkService
    {
        public PoolStateResult GetPoolState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_pool_state";

            var state = HttpRequestHelper.Request<JsonRpcResult<PoolStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);

            return state.Result;
        }

        public PeerResult GetPeers(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_peers";

            var peers = HttpRequestHelper.Request<JsonRpcResult<PeerResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);
            
            return peers.Result;
        }
    }
}