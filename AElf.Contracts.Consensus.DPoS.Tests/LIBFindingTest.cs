using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public class LIBFindingTest
    {
        private ContractTester<DPoSContractTestAElfModule> Starter { get; }

        private List<ContractTester<DPoSContractTestAElfModule>> Miners { get; }

        private const int MinersCount = 17;

        private const int MiningInterval = 1;

        public LIBFindingTest()
        {
            // The starter initial chain and tokens.
            Starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, MinersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            AsyncHelper.RunSync(() => Starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval));
            Miners = Enumerable.Range(0, 17)
                .Select(i => Starter.CreateNewContractTester(minersKeyPairs[i])).ToList();
        }

        [Fact]
        public async Task GetLIBOffsetTest()
        {
            // No consensus information, offset should be 0.
            {
                var offset = await Miners.AnyOne().GetLIBOffset();
                offset.ShouldBe(0L);
            }

            var minimumCount = ((int) ((MinersCount * 2d) / 3)) + 1;

            // Not enough for LIB, offset should be 0.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(minimumCount - 1);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(0L);
            }

            // Not enough for LIB.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(1);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(minimumCount);
            }

            // Surpass the LIB.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(1);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(minimumCount);
            }

            // Run to next round.
            {
                await Miners.ProduceNormalBlocks(MinersCount);
                var extraBlockMiner = await Miners.ChangeRoundAsync();

                var offset = await extraBlockMiner.GetLIBOffset();
                offset.ShouldBe(minimumCount);
            }

            // Keep mining blocks.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(MinersCount / 2);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(minimumCount);
            }

            // Suddenly some miners become offline.
            {
                var extraBlockMiner = await Miners.ChangeRoundAsync();

                var offset = await extraBlockMiner.GetLIBOffset();
                offset.ShouldBe(0L);
            }

            // Miners online but not enough for LIB.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(MinersCount / 2);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(0L);
            }

            // Miners online and enough for LIB.
            {
                var finalMiner = await Miners.ProduceNormalBlocks(MinersCount / 2 + 1);

                var offset = await finalMiner.GetLIBOffset();
                offset.ShouldBe(minimumCount);
            }
        }
    }
}