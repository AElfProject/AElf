using System;
using System.Threading.Tasks;
using AElf.Configuration;
using Community.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;
using AElf.Kernel;
using AElf.RPC;

namespace AElf.ChainController.Rpc
{
    [Path("")]
    public partial class ChainControllerRpcService : IJsonRpcService
    {
        public INodeConfig NodeConfig { get; set; }
        public  IChainService ChainService { get; set; }
        public IChainCreationService ChainCreationService { get; set; }

        private Hash GetGenesisContractHash(SmartContractType contractType)
        {
            return ChainCreationService.GenesisContractHash(NodeConfig.ChainId, contractType);
        }

        [JsonRpcMethod("connect_chain")]
        public Task<JObject> ProGetChainInfo()
        {
            try
            {
                var chainId = NodeConfig.ChainId;
                var basicContractZero = GetGenesisContractHash(SmartContractType.BasicContractZero);
                var tokenContract = GetGenesisContractHash(SmartContractType.TokenContract);
                var response = new JObject
                {
                    [SmartContractType.BasicContractZero.ToString()] = basicContractZero.ToHex(),
                    [SmartContractType.TokenContract.ToString()] = tokenContract.ToHex(),
                    ["chain_id"] = chainId.ToHex()
                };

                return Task.FromResult(JObject.FromObject(response));
            }
            catch (Exception e)
            {  var response = new JObject
                {
                    ["exception"] = e.ToString()
                };

                return Task.FromResult(JObject.FromObject(response));   
            }
        }
    }
}