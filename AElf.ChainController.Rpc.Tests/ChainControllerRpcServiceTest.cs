using System;
using System.Net.Http;
using System.Net.Http.Formatting;
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

        private ILogger<ChainControllerRpcServiceTest> _logger;


        [Fact]
        public async Task TestGetBlockHeight()
        {
            var height = await _chainControllerRpcService.ProGetBlockHeight();
        }

        public ChainControllerRpcServiceTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _chainControllerRpcService = GetRequiredService<ChainControllerRpcService>();
            _logger = GetService<ILogger<ChainControllerRpcServiceTest>>() ??
                      NullLogger<ChainControllerRpcServiceTest>.Instance;
        }
    }

    public class ChainControllerRpcServiceServerTest : RpcTestBase
    {
        private ILogger<ChainControllerRpcServiceServerTest> _logger;

        public ChainControllerRpcServiceServerTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            
            _logger = GetService<ILogger<ChainControllerRpcServiceServerTest>>() ??
                      NullLogger<ChainControllerRpcServiceServerTest>.Instance;
        }

        [Fact]
        public async Task TestGetBlockHeight()
        {
            var response = await JsonCallAsJObject("/chain", "get_block_height");

            _logger.LogInformation(response.ToString());

            var height = (int) response["result"]["result"]["block_height"];

            height.ShouldBeGreaterThanOrEqualTo(0);
        }

        
        [Fact]
        public async Task TestGetBlockHeight2()
        {
            var response = await JsonCallAsJObject("/chain", "get_block_height");

            _logger.LogInformation(response.ToString());

            var height = (int) response["result"]["result"]["block_height"];

            height.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}