using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;
using Microsoft.Extensions.Options;

namespace AElf.Management.Services
{
    public class AkkaService : IAkkaService
    {
        private readonly ManagementOptions _managementOptions;

        public AkkaService(IOptionsSnapshot<ManagementOptions> options)
        {
            _managementOptions = options.Value;
        }

        public async Task<List<ActorStateResult>> GetState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "akkastate";

            var state = await HttpRequestHelper.Request<JsonRpcResult<List<ActorStateResult>>>(_managementOptions
                .ServiceUrls[chainId].RpcAddress, jsonRpcArg);

            return state.Result;
        }
    }
}