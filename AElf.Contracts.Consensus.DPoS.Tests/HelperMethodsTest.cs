using System.Threading.Tasks;
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
    }
}