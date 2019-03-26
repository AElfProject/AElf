using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class DPoSSideChainTests : DPoSSideChainTestBase
    {
        private DPoSSideChainTester TesterManager { get; set; }

        public DPoSSideChainTests()
        {
            TesterManager = new DPoSSideChainTester();
            TesterManager.InitialSingleTester();
        }

        [Fact]
        public async Task Initial_GetConsensusCommand()
        {
            //test with not boot miner
            var queryTriggerInformation =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(TesterManager.SingleTester.PublicKey, DateTime.UtcNow.ToTimestamp(), false);

            var bytes = await TesterManager.SingleTester.CallContractMethodAsync(TesterManager.DPoSSideChainContractAddress,
                "GetConsensusCommand", queryTriggerInformation);
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);
            
            //Assert
            actual.TimeoutMilliseconds.ShouldBe(int.MaxValue);
            actual.CountingMilliseconds.ShouldBe(int.MaxValue);
            var behavior = DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour;
            behavior.ShouldBe(DPoSBehaviour.InitialConsensus);
            
            //test with boot miner 
            queryTriggerInformation =
                TesterManager.GetTriggerInformationForNextRoundOrTerm(TesterManager.SingleTester.PublicKey, DateTime.UtcNow.ToTimestamp(), true);

            bytes = await TesterManager.SingleTester.CallContractMethodAsync(TesterManager.DPoSSideChainContractAddress,
                "GetConsensusCommand", queryTriggerInformation);
            actual = ConsensusCommand.Parser.ParseFrom(bytes);
            
            //Assert
            actual.TimeoutMilliseconds.ShouldBe(int.MaxValue);
            actual.CountingMilliseconds.ShouldBe(8000);
            behavior = DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour;
            behavior.ShouldBe(DPoSBehaviour.InitialConsensus);
        }

        [Fact]
        public async Task Initial_GetNewConsensusInformation()
        {
            TesterManager.InitialTesters();
            
            var triggerInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            // Act
            var bytes = await TesterManager.Testers[0].CallContractMethodAsync(TesterManager.DPoSSideChainContractAddress,
                ConsensusConsts.GetNewConsensusInformation, triggerInformation);
            var information = DPoSInformation.Parser.ParseFrom(bytes);
            var round = information.Round;

            // Assert
            Assert.Equal(TesterManager.Testers[0].KeyPair.PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialConsensus, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(TesterManager.MinersCount,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
        }

        [Fact]
        public async Task Initial_GetNewConsensusInformation_ExtensionMethod()
        {
            TesterManager.InitialTesters();
            
            var stubInitialInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            // Act
            var information = await TesterManager.Testers[0].GetNewConsensusInformationAsync(stubInitialInformation);
            var round = information.Round;

            // Assert
            Assert.Equal(TesterManager.Testers[0].KeyPair.PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialConsensus, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(TesterManager.MinersCount,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
        }
        
        [Fact]
        public async Task Initial_GenerateConsensusTransactions()
        {
            TesterManager.InitialTesters();
            
            var stubInitialExtraInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            // Act
            var bytes = await TesterManager.Testers[0].CallContractMethodAsync(TesterManager.DPoSSideChainContractAddress,
                ConsensusConsts.GenerateConsensusTransactions,
                stubInitialExtraInformation);
            var initialTransactions = TransactionList.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(),
                initialTransactions.Transactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(TesterManager.Testers[0].KeyPair.PublicKey),
                initialTransactions.Transactions.First().From);
        }

        [Fact]
        public async Task Initial_GenerateConsensusTransactions_ExtensionMethod()
        {
            TesterManager.InitialTesters();
            
            var stubInitialExtraInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            // Act
            var initialTransactions =
                await TesterManager.Testers[0].GenerateConsensusTransactionsAsync(stubInitialExtraInformation);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(), initialTransactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(TesterManager.Testers[0].KeyPair.PublicKey),
                initialTransactions.First().From);
        }
        
        [Fact]
        public async Task NormalBlock_ValidationConsensus_Success()
        {
            TesterManager.InitialTesters();

            var stubInitialExtraInformation = TesterManager.GetTriggerInformationForInitialTerm(TesterManager.MinersKeyPairs);

            await TesterManager.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, TesterManager.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                TesterManager.GetTriggerInformationForNormalBlock(TesterManager.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            var newInformation =
                await TesterManager.Testers[1].GetNewConsensusInformationAsync(triggerInformationForNormalBlock);

            // Act
            var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }
    }
}