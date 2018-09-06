using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.AspNetCore.JsonRpc;

namespace AElf.Concurrency.Manager
{
    public class ActorService :IJsonRpcService
    {
        [JsonRpcMethod("actorstate")]
        public Task<List<MemberInfo>> ActorState()
        {
            return Task.FromResult(ClusterState.MemberInfos.Values.ToList());
        }
    }
}