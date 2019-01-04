using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AElf.Net.Rpc;
using AElf.RPC.Tests;
using Anemonis.JsonRpc;
using Xunit;

namespace AElf.ChainController.Rpc.Tests
{
    //Application Service Test should move out of this project
    public class ChainControllerRpcServiceTest : RpcTestBase
    {
        private ChainControllerRpcService _chainControllerRpcService;

        public ChainControllerRpcServiceTest()
        {
            _chainControllerRpcService = GetRequiredService<ChainControllerRpcService>();
        }

        [Fact]
        public async Task TestGetBlockHeight()
        {
            
            var height =  await _chainControllerRpcService.ProGetBlockHeight();
        }
        
        
    }

    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        [Fact]
        public async Task TestGetBlockHeight()
        {
            //var response2 = await CreateJsonRpcClient("path").InvokeAsync<long>("get_block_height", new JsonRpcId(1));
            
            //Todo: should make a base method to post json
            var response = await Client.PostAsync("/chain",
                new StringContent("{\n  \"jsonrpc\": \"2.0\",\n  \"id\": 1,\n  \"method\": \"get_block_height\",\n  \"params\": {}\n}",Encoding.UTF8, "application/json"));

            await Task.Delay(10000000);
            throw new Exception(response.StatusCode.ToString());
            //

        }
    }
}