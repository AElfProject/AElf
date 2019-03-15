using System.Linq;
using System.Threading.Tasks;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Types.CSharp;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class HelperMethodsTest
    {
        [Fact]
        public async Task SetBlockchainAgeTest()
        {
            const ulong age = 100UL;
            var starter = new ContractTester<DPoSContractTestAElfModule>();
            await starter.InitialChainAndTokenAsync();
            await starter.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.SetBlockchainAge),
                age);

            // Starter can set blockchain age.
            {
                var blockchainAge = (await starter.CallContractMethodAsync(starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetBlockchainAge))).DeserializeToUInt64();

                blockchainAge.ShouldBe(age);
            }

            var user = starter.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            await user.SetBlockchainAgeAsync(age + 100);

            // While others can't.
            {
                var blockchainAge = (await user.CallContractMethodAsync(starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetBlockchainAge))).DeserializeToUInt64();

                // Still 100.
                blockchainAge.ShouldBe(age);
            }
        }

        [Fact]
        public async Task RunConsensusTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;
            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs, miningInterval);
            var miners = Enumerable.Range(0, 17)
                .Select(i => starter.CreateNewContractTester(minersKeyPairs[i])).ToList();

            await miners.RunConsensusAsync(1);

            // Check current round information.
            {
                var round = await miners.AnyOne().GetCurrentRoundInformationAsync();
            
                Assert.Equal(2UL, round.RoundNumber);
            }

            await miners.RunConsensusAsync(2, true);

            // Check current round information.
            {
                var round = await miners.AnyOne().GetCurrentRoundInformationAsync();

                Assert.Equal(4UL, round.RoundNumber);
                Assert.Equal(2UL, round.TermNumber);
            }
        }

        [Fact]
        public async Task ChangeTermTest()
        {
            
        }
    }
}