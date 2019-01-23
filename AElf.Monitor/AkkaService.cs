using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.RPC;
using Community.AspNetCore.JsonRpc;

namespace AElf.Monitor
{
    [Path("/monitor")]
    public class AkkaService :IJsonRpcService
    {
        [JsonRpcMethod("clusterstate")]
        public Task<List<MemberInfo>> ClusterState()
        {
            return Task.FromResult(AkkaClusterState.MemberInfos.Values.ToList());
        }
    }
}