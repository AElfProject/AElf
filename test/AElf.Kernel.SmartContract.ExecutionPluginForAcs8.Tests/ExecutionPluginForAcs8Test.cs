using System;
using System.Threading.Tasks;
using Acs8;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    public class ExecutionPluginForAcs8Test : ExecutionPluginForAcs8TestBase
    {
        public ExecutionPluginForAcs8Test()
        {
            AsyncHelper.RunSync(InitializeContracts);
        }

        [Fact]
        public async Task SetResourceTokenBuyingPreferences()
        {
            const long threshold = 100_00000000;
            const long amount = 1_000_00000000;
            var preferences = new ResourceTokenBuyingPreferences
            {
                CpuThreshold = threshold,
                StoThreshold = threshold,
                NetThreshold = threshold,
                CpuAmount = amount,
                StoAmount = amount,
                NetAmount = amount,
                PayLimit = 1_000_000_00000000
            };
            await DefaultTester.SetResourceTokenBuyingPreferences.SendAsync(preferences);
            
            // Check result.
            var result = await DefaultTester.GetResourceTokenBuyingPreferences.CallAsync(new Empty());

            result.ShouldBe(preferences);
        }
    }
}