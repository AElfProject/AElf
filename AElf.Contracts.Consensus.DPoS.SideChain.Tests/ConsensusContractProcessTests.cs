using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class ConsensusContractProcessTests : DPoSSideChainTestBase
    {
        private const int MiningInterval = 4000;
        private readonly DPoSSideChainTester TesterManager;
        
        public ConsensusContractProcessTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }

        [Fact]
        public async Task InitialConsensus_WithException()
        {
            TesterManager.InitialTesters();
            
            //Incorrect round number at beginning.
            var input = new Round
            {
                RoundNumber = 2
            };
            var transactionResult = await TesterManager.SingleTester.ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "InitialConsensus", input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Incorrect round information: invalid round number.").ShouldBeTrue();
            
            //Correct round number
            input = new Round
            {
                RoundNumber = 1
            };
            transactionResult = await TesterManager.SingleTester.ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "InitialConsensus", input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Incorrect round information: no miner.").ShouldBeTrue();
        }

        [Fact]
        public async Task InitialConsensus_Success()
        {
            TesterManager.InitialTesters();
            
            // Act
            var input = TesterManager.MinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList().ToMiners()
                .GenerateFirstRoundOfNewTerm(MiningInterval);
            var transactionResult = await TesterManager.Testers[0].ExecuteContractWithMiningAsync(
                TesterManager.DPoSSideChainContractAddress,
                "InitialConsensus", input);
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        [Fact]
        public async Task NormalBlock_GetConsensusCommand()
        {
            TesterManager.InitialTesters();

            var triggerInformationForInitialTerm = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);
            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, TesterManager.Testers[1]);

            // Act
            var actual = await TesterManager.Testers[1].GetConsensusCommandAsync();

            // Assert
            Assert.Equal(DPoSBehaviour.UpdateValue, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.CountingMilliseconds != MiningInterval);
        }

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            TesterManager.InitialTesters();

            var stubInitialExtraInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, TesterManager.Testers[1]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation =
                await TesterManager.Testers[1].GetNewConsensusInformationAsync(stubExtraInformation);

            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.Round
                .RealTimeMinersInformation[TesterManager.Testers[1].KeyPair.PublicKey.ToHex()]
                .OutValue);
        }

        [Fact]
        public async Task NormalBlock_GenerateConsensusTransactions()
        {
            TesterManager.InitialTesters();

            var stubInitialExtraInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, TesterManager.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

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

            var triggerInformationForInitialTerm = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);
            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, TesterManager.Testers[1]);

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 1).ToTimestamp();
            var command = await TesterManager.Testers[0].GetConsensusCommandAsync(futureTime);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            Assert.True(command.CountingMilliseconds > 0);
            Assert.Equal(4000, command.TimeoutMilliseconds);
        }

        [Fact]
        public async Task NextRound_GetNewConsensusInformation()
        {
            TesterManager.InitialTesters();

            var triggerInformationForInitialTerm = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, TesterManager.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

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

            var triggerInformationForInitialTerm = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, TesterManager.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * TesterManager.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var consensusTransactions = await TesterManager.Testers[1]
                .GenerateConsensusTransactionsAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound.ToString(), consensusTransactions.First().MethodName);
        }
    }
}