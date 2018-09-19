using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class NodeService : INodeService
    {
        public bool IsAlive(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "dpos_isalive";

            var state = HttpRequestHelper.Request<JsonRpcResult<DposStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsAlive;
        }
        
        public bool IsForked(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "node_isforked";

            var state = HttpRequestHelper.Request<JsonRpcResult<NodeStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsForked;
        }
    }
}