using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class AkkaService:IAkkaService
    {
        public async Task<List<ActorStateResult>> GetState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "akkastate";

            var state = await HttpRequestHelper.Request<JsonRpcResult<List<ActorStateResult>>>(ServiceUrlHelper.GetMonitorRpcAddress(chainId), jsonRpcArg);

            return state.Result;
        }
    }
}