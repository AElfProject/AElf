using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class ConsensusContractProcessTests : DPoSSideChainTestBase
    {
        private readonly DPoSSideChainTester TesterManager;

        public ConsensusContractProcessTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }

        [Fact]
        public async Task UpdateValue_WithError()
        {
            TesterManager.InitialTesters();

            //Id not match
            {
                var input = new ToUpdate
                {
                    RoundId = 10
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.UpdateValue), input);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Round information not found.").ShouldBeTrue();
            }

            //round information not found
            {
                var roundInput = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                    .GenerateFirstRoundOfNewTerm(DPoSSideChainTester.MiningInterval, DateTime.UtcNow);
                await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.InitialConsensus), roundInput);

                var input = new ToUpdate
                {
                    RoundId = 2,
                    ActualMiningTime = DateTime.UtcNow.ToTimestamp(),
                    OutValue = Hash.Generate()
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.UpdateValue), input);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Round Id not matched.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task UpdateValue_Success()
        {
            TesterManager.InitialTesters();

            //init round info 
            var roundInput = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                .GenerateFirstRoundOfNewTerm(DPoSSideChainTester.MiningInterval, DateTime.UtcNow);
            await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.InitialConsensus), roundInput);

            //query current round info
            var roundInfo = await TesterManager.Testers[0].GetCurrentRoundInformation();

            var input = new ToUpdate
            {
                RoundId = roundInfo.RoundId,
                ActualMiningTime = DateTime.UtcNow.ToTimestamp(),
                OutValue = Hash.Generate()
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.UpdateValue), input);
            //Assert
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var queryRound = await TesterManager.Testers[0].GetCurrentRoundInformation();
            queryRound.RealTimeMinersInformation[TesterManager.Testers[0].PublicKey].OutValue.ShouldBe(input.OutValue);
            queryRound.RealTimeMinersInformation[TesterManager.Testers[0].PublicKey].ActualMiningTime
                .ShouldBe(input.ActualMiningTime);
        }

        [Fact]
        public async Task NextRound_Failed_With_Incorrect_RoundNumber()
        {
            TesterManager.InitialTesters();

            var roundInput = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                .GenerateFirstRoundOfNewTerm(DPoSSideChainTester.MiningInterval, DateTime.UtcNow);
            await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.InitialConsensus), roundInput);
            var input = new Round
            {
                RoundNumber = -1
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Incorrect round number for next round.").ShouldBeTrue();
        }

        [Fact]
        public async Task NextRound_Failed_With_Incorrect_RoundInformation()
        {
            TesterManager.InitialTesters();

            var input = new Round
            {
                RoundNumber = 2,
                BlockchainAge = 1
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Failed to get current round information.").ShouldBeTrue();
        }

        [Fact]
        public async Task NextRound_Success()
        {
            TesterManager.InitialTesters();

            var roundInput = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                .GenerateFirstRoundOfNewTerm(DPoSSideChainTester.MiningInterval);
            await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.InitialConsensus), roundInput);
            var roundInfo = await TesterManager.Testers[0].GetCurrentRoundInformation();

            roundInfo.RoundNumber = 2;
            roundInfo.BlockchainAge = 14;
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), roundInfo);

            //Assert
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var queryRound = await TesterManager.Testers[0].GetCurrentRoundInformation();
            queryRound.RoundNumber.ShouldBe(2);
            queryRound.BlockchainAge.ShouldBe(14);
        }

        [Fact]
        public async Task NormalBlock_GetConsensusCommand()
        {
            TesterManager.InitialTesters();

            // Act
            var actual = await TesterManager.Testers[1].GetConsensusCommandAsync();

            // Assert
            Assert.Equal(DPoSBehaviour.UpdateValue, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.NextBlockMiningLeftMilliseconds != DPoSSideChainTester.MiningInterval);
        }

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            TesterManager.InitialTesters();

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(),
                    inValue);

            // Act
            var newConsensusInformation =
                await TesterManager.Testers[1].GetNewConsensusInformationAsync(stubExtraInformation);

            // Assert
            Assert.NotNull(newConsensusInformation);
            var resultOutValue = newConsensusInformation.Round
                .RealTimeMinersInformation[TesterManager.Testers[1].KeyPair.PublicKey.ToHex()].OutValue;
            Assert.Equal(outValue, resultOutValue);
        }

        [Fact]
        public async Task NormalBlock_GenerateConsensusTransactions()
        {
            TesterManager.InitialTesters();

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(),
                    inValue);

            // Act
            var consensusTransactions =
                await TesterManager.Testers[1].GenerateConsensusTransactionsAsync(triggerInformationForNormalBlock);

            // Assert
            Assert.NotNull(consensusTransactions);
            Assert.Equal(DPoSBehaviour.UpdateValue.ToString(), consensusTransactions.First().MethodName);
        }

        [Fact]
        public async Task NextRound_GetConsensusCommand()
        {
            TesterManager.InitialTesters();

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 1).ToTimestamp();
            var command = await TesterManager.Testers[0].GetConsensusCommandAsync(futureTime);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            Assert.True(command.NextBlockMiningLeftMilliseconds > 0);
            Assert.Equal(4000, command.LimitMillisecondsOfMiningBlock);
        }

        [Fact]
        public async Task NextRound_GetNewConsensusInformation()
        {
            TesterManager.InitialTesters();

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(
                    TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var newConsensusInformation =
                await TesterManager.Testers[1].GetNewConsensusInformationAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(2L, newConsensusInformation.Round.RoundNumber);
        }

        [Fact]
        public async Task NextRound_GenerateConsensusTransactions()
        {
            TesterManager.InitialTesters();

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(
                    TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var consensusTransactions = await TesterManager.Testers[1]
                .GenerateConsensusTransactionsAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound.ToString(), consensusTransactions.First().MethodName);
        }
    }
}