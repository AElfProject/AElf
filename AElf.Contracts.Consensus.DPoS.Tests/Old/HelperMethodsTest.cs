using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class HelperMethodsTest
    {
        [Fact]
        public async Task SetBlockchainAgeTest()
        {
            const long age = 100L;
            var starter = new ContractTester<DPoSContractTestAElfModule>();
            var initialMiners = Enumerable.Range(0, 3).Select(_ => CryptoHelpers.GenerateKeyPair())
                .ToList();
            await starter.InitialChainAndTokenAsync(initialMiners);
            var initialMiner = starter.CreateNewContractTester(initialMiners[0]);
            await initialMiner.ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.SetBlockchainAge),
                new SInt64Value(){Value = age});

            // Starter can set blockchain age.
            {
                var blockchainAge = SInt64Value.Parser.ParseFrom(await starter.CallContractMethodAsync(
                    starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.GetBlockchainAge),
                    new Empty())).Value;

                blockchainAge.ShouldBe(age);
            }

            var user = starter.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            await user.SetBlockchainAgeAsync(age + 100);
        }

        [Fact]
        public async Task RunConsensusTest()
        {
            const int minersCount = 3;
            const int miningInterval = 4000;
            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs, miningInterval, DateTime.UtcNow.ToTimestamp());
            var miners = Enumerable.Range(0, minersCount)
                .Select(i => starter.CreateNewContractTester(minersKeyPairs[i])).ToList();

            await miners.RunConsensusAsync(1);

            // Check current round information.
            {
                var round = await miners.AnyOne().GetCurrentRoundInformationAsync();
            
                Assert.Equal(2L, round.RoundNumber);
            }

            await miners.RunConsensusAsync(2, true);

            // Check current round information.
            {
                var round = await miners.AnyOne().GetCurrentRoundInformationAsync();

                Assert.Equal(4L, round.RoundNumber);
                Assert.Equal(2L, round.TermNumber);
            }
        }

        [Fact]
        public async Task ChangeTermTest()
        {
            
        }
    }
}