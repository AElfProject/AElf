using System.Collections.Generic;
using System.Net;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class AkkaService:IAkkaService
    {
        public List<MemberInfo> GetState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "akkastate";

            var state = HttpRequestHelper.Request<ActorStateResult>("http://127.0.0.1:9099", jsonRpcArg);

            return state.Result;
        }
    }
}