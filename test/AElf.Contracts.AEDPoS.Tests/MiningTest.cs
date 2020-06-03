using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoS
{
    public class MiningTest : AEDPoSContractTestBase<AEDPoSContractTestAElfModule>
    {
        [Fact]
        public async Task ConsensusProcessTest()
        {
            await AssertBestChainHeight(1);
            await PackageConsensusTransactionAsync();
            await AssertBestChainHeight(2);
            for (var i = 0; i < 100; i++)
            {
                await PackageConsensusTransactionAsync();
            }
            await AssertBestChainHeight(102);
        }

        private async Task AssertBestChainHeight(long height)
        {
            (await BlockchainService.GetChainAsync()).BestChainHeight.ShouldBe(height);
        }
    }
}