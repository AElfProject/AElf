using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AElf.Net.Rpc;
using AElf.RPC.Tests;
using Anemonis.JsonRpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.ChainController.Rpc.Tests
{
    //Application Service Test should move out of this project
    public class ChainControllerRpcServiceTest : RpcTestBase
    {
        private ChainControllerRpcService _chainControllerRpcService;

        public ILogger<ChainControllerRpcServiceTest> Logger { get; set; }


        [Fact]
        public async Task TestGetBlockHeight()
        {
            var height = await _chainControllerRpcService.GetBlockHeight();
        }

        public ChainControllerRpcServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _chainControllerRpcService = GetRequiredService<ChainControllerRpcService>();
            Logger = GetService<ILogger<ChainControllerRpcServiceTest>>() ??
                      NullLogger<ChainControllerRpcServiceTest>.Instance;
        }
    }

    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        public ILogger<ChainControllerRpcServiceServerTest> Logger { get; set; }

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            
            Logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                      NullLogger<ChainControllerRpcServiceServerTest>.Instance;
        }

        [Fact]
        public async Task TestGetBlockHeight()
        {
            var response = await JsonCallAsJObject("/chain", "get_block_height");

            Logger.LogInformation(response.ToString());

            var height = (int) response["result"]["result"]["block_height"];

            height.ShouldBeGreaterThanOrEqualTo(0);
        }

        
        [Fact]
        public async Task TestGetBlockHeight2()
        {
            var response = await JsonCallAsJObject("/chain", "get_block_height");

            Logger.LogInformation(response.ToString());

            var height = (int) response["result"]["result"]["block_height"];

            height.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}