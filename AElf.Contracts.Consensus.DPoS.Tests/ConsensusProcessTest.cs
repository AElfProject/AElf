using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public class ConsensusProcessTest
    {
        private int _miningInterval = 4000;

        [Fact]
        public async Task Initial_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialSingleTester();

            var firstTriggerInformation = new DPoSTriggerInformation
            {
                PublicKey = testers.SingleTester.CallOwnerKeyPair.PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                IsBootMiner = true,
            };
            var bytes = await testers.SingleTester.CallContractMethodAsync(testers.ConsensusContractAddress,
                ConsensusConsts.GetConsensusCommand, firstTriggerInformation.ToByteArray());
            var actual = ConsensusCommand.Parser.ParseFrom(bytes);

            // Assert
            Assert.True(8000 >= actual.CountingMilliseconds); //For now the 8000 is hard coded in contract code.
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialConsensus, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }

        [Fact]
        public async Task Initial_GetConsensusCommand_ExtensionMethod()
        {
            var testers = new ConsensusTesters();
            testers.InitialSingleTester();

            var actual = await testers.SingleTester.GetConsensusCommand();

            // Assert
            Assert.True(8000 >= actual.CountingMilliseconds);
            Assert.Equal(int.MaxValue, actual.TimeoutMilliseconds);
            Assert.Equal(DPoSBehaviour.InitialConsensus, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
        }

        [Fact]
        public async Task Initial_GetNewConsensusInformation()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            // Act
            var bytes = await testers.Testers[0].CallContractMethodAsync(testers.ConsensusContractAddress,
                ConsensusConsts.GetNewConsensusInformation, triggerInformation.ToByteArray());
            var information = DPoSInformation.Parser.ParseFrom(bytes);
            var round = information.Round;

            // Assert
            Assert.Equal(testers.Testers[0].CallOwnerKeyPair.PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialConsensus, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(testers.MinersCount,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
        }

        [Fact]
        public async Task Initial_GetNewConsensusInformation_ExtensionMethod()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            // Act
            var information = await testers.Testers[0].GetNewConsensusInformation(stubInitialInformation);
            var round = information.Round;

            // Assert
            Assert.Equal(testers.Testers[0].CallOwnerKeyPair.PublicKey.ToHex(), information.SenderPublicKey);
            Assert.Equal(DPoSBehaviour.InitialConsensus, information.Behaviour);
            // Check the basic information of first round.
            Assert.True(1 == round.RoundNumber);
            Assert.Equal(testers.MinersCount,
                round.RealTimeMinersInformation.Values.Count(m =>
                    m.Signature != null && m.ExpectedMiningTime != null && m.Order > 0 && m.PromisedTinyBlocks == 1));
        }

        [Fact]
        public async Task Initial_GenerateConsensusTransactions()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            // Act
            var bytes = await testers.Testers[0].CallContractMethodAsync(testers.ConsensusContractAddress,
                ConsensusConsts.GenerateConsensusTransactions,
                stubInitialExtraInformation.ToByteArray());
            var initialTransactions = TransactionList.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(), initialTransactions.Transactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(testers.Testers[0].CallOwnerKeyPair.PublicKey),
                initialTransactions.Transactions.First().From);
        }

        [Fact]
        public async Task Initial_GenerateConsensusTransactions_ExtensionMethod()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            // Act
            var initialTransactions =
                await testers.Testers[0].GenerateConsensusTransactions(stubInitialExtraInformation);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(), initialTransactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(testers.Testers[0].CallOwnerKeyPair.PublicKey),
                initialTransactions.First().From);
        }

        [Fact]
        public async Task NormalBlock_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);
            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(triggerInformationForInitialTerm, testers.Testers[1]);

            // Act
            var actual = await testers.Testers[1].GetConsensusCommand();

            // Assert
            Assert.Equal(DPoSBehaviour.UpdateValue, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.CountingMilliseconds != _miningInterval);
        }

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation = await testers.Testers[1].GetNewConsensusInformation(stubExtraInformation);

            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.Round
                .RealTimeMinersInformation[testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex()]
                .OutValue);
        }

        [Fact]
        public async Task NormalBlock_ValidationConsensus_Success()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            var newInformation = await testers.Testers[1].GetNewConsensusInformation(triggerInformationForNormalBlock);

            // Act
            var validationResult = await testers.Testers[0].ValidateConsensus(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact]
        public async Task NormalBlock_GenerateConsensusTransactions()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex(), inValue);

            // Act
            var consensusTransactions =
                await testers.Testers[1].GenerateConsensusTransactions(triggerInformationForNormalBlock);

            // Assert
            Assert.NotNull(consensusTransactions);
            Assert.Equal(DPoSBehaviour.UpdateValue.ToString(), consensusTransactions.First().MethodName);
        }

        [Fact]
        public async Task ExtraBlock_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);
            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(triggerInformationForInitialTerm, testers.Testers[1]);

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 1).ToTimestamp();
            var actual = await testers.Testers[0].GetConsensusCommand(futureTime);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.CountingMilliseconds > 0);
            Assert.Equal(4000, actual.TimeoutMilliseconds);
        }
        
        [Fact]
        public async Task ExtraBlock_GetNewConsensusInformation()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(triggerInformationForInitialTerm, testers.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRoundOrTerm(testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var newConsensusInformation = await testers.Testers[1].GetNewConsensusInformation(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(2UL,  newConsensusInformation.Round.RoundNumber);
        }
        
        [Fact]
        public async Task ExtraBlock_GenerateConsensusTransactions()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlock(triggerInformationForInitialTerm, testers.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRoundOrTerm(testers.Testers[1].CallOwnerKeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var consensusTransactions = await testers.Testers[1].GenerateConsensusTransactions(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound.ToString(),  consensusTransactions.First().MethodName);
        }

        private DPoSTriggerInformation GetTriggerInformationForInitialTerm(IReadOnlyList<ECKeyPair> stubMiners)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = stubMiners[0].PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Miners = {stubMiners.Select(m => m.PublicKey.ToHex()).ToList()},
                MiningInterval = 4000,
            };
        }

        private DPoSTriggerInformation GetTriggerInformationForNormalBlock(string publicKey, Hash currentInValue,
            Hash previousInValue = null)
        {
            if (previousInValue == null)
            {
                previousInValue = Hash.Default;
            }

            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                PreviousInValue = previousInValue,
                CurrentInValue = currentInValue
            };
        }

        private DPoSTriggerInformation GetTriggerInformationForNextRoundOrTerm(string publicKey, Timestamp timestamp)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = publicKey,
                Timestamp = timestamp
            };
        }
    }

    internal class ConsensusTesters
    {
        public int MinersCount { get; set; } = 3;

        public int ChainId { get; set; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        public List<ECKeyPair> MinersKeyPairs { get; set; } = new List<ECKeyPair>();

        public List<ContractTester> Testers { get; set; } = new List<ContractTester>();
        
        public ContractTester SingleTester { get; set; }

        public Address ConsensusContractAddress { get; set; }

        public void InitialTesters()
        {
            for (var i = 0; i < MinersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                MinersKeyPairs.Add(keyPair);
                var tester = new ContractTester(ChainId, keyPair);

                AsyncHelper.RunSync(
                    () => tester.InitialChainAsync());
                Testers.Add(tester);
            }

            ConsensusContractAddress = Testers[0].GetConsensusContractAddress();
        }
        
        public void InitialSingleTester()
        {
            SingleTester = new ContractTester(ChainId, CryptoHelpers.GenerateKeyPair());
            AsyncHelper.RunSync(
                () => SingleTester.InitialChainAsync());
            ConsensusContractAddress = SingleTester.GetConsensusContractAddress();
        }
    }
}