using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Rpc.ChainController.Tests
{
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
            // Prepare chain data
            var chainManager = GetRequiredService<ChainManager>();
            await chainManager.CreateAsync(ChainHelpers.ConvertBase58ToChainId("AELF"), Hash.Genesis);

            // Verify
            var response = await JsonCallAsJObject("/chain", "GetBlockHeight");
            Logger.LogInformation(response.ToString());
            var height = (int) response["result"];
            height.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}