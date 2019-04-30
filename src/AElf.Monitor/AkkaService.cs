using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.OS.Rpc;
using Anemonis.AspNetCore.JsonRpc;

namespace AElf.Monitor
{
    [Path("/monitor")]
    public class AkkaService :IJsonRpcService
    {
        [JsonRpcMethod("AkkaState")]
        public Task<List<MemberInfo>> ClusterState()
        {
            return Task.FromResult(AkkaClusterState.MemberInfos.Values.ToList());
        }
    }
}