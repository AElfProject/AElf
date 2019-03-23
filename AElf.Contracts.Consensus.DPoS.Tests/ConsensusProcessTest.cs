using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ConsensusProcessTest : ContractTestBase<DPoSContractTestAElfModule>
    {
        private const int MiningInterval = 4000;

        [Fact]
        public async Task Initial_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialSingleTester();

            var firstTriggerInformation = new DPoSTriggerInformation
            {
                PublicKey = testers.SingleTester.KeyPair.PublicKey.ToHex(),
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                IsBootMiner = true,
            };
            var bytes = await testers.SingleTester.CallContractMethodAsync(testers.ConsensusContractAddress,
                ConsensusConsts.GetConsensusCommand, firstTriggerInformation);
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

            var actual = await testers.SingleTester.GetConsensusCommandAsync();

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
                ConsensusConsts.GetNewConsensusInformation, triggerInformation);
            var information = DPoSInformation.Parser.ParseFrom(bytes);
            var round = information.Round;

            // Assert
            Assert.Equal(testers.Testers[0].KeyPair.PublicKey.ToHex(), information.SenderPublicKey);
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
            var information = await testers.Testers[0].GetNewConsensusInformationAsync(stubInitialInformation);
            var round = information.Round;

            // Assert
            Assert.Equal(testers.Testers[0].KeyPair.PublicKey.ToHex(), information.SenderPublicKey);
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
                stubInitialExtraInformation);
            var initialTransactions = TransactionList.Parser.ParseFrom(bytes);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(),
                initialTransactions.Transactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(testers.Testers[0].KeyPair.PublicKey),
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
                await testers.Testers[0].GenerateConsensusTransactionsAsync(stubInitialExtraInformation);

            // Assert
            Assert.Equal(DPoSBehaviour.InitialConsensus.ToString(), initialTransactions.First().MethodName);
            Assert.Equal(Address.FromPublicKey(testers.Testers[0].KeyPair.PublicKey),
                initialTransactions.First().From);
        }

        [Fact]
        public async Task NormalBlock_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);
            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, testers.Testers[1]);

            // Act
            var actual = await testers.Testers[1].GetConsensusCommandAsync();

            // Assert
            Assert.Equal(DPoSBehaviour.UpdateValue, DPoSHint.Parser.ParseFrom(actual.Hint).Behaviour);
            Assert.True(actual.CountingMilliseconds != MiningInterval);
        }

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation =
                await testers.Testers[1].GetNewConsensusInformationAsync(stubExtraInformation);

            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.Round
                .RealTimeMinersInformation[testers.Testers[1].KeyPair.PublicKey.ToHex()]
                .OutValue);
        }

        [Fact]
        public async Task NormalBlock_ValidationConsensus_Success()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var stubInitialExtraInformation = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            var newInformation =
                await testers.Testers[1].GetNewConsensusInformationAsync(triggerInformationForNormalBlock);

            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);

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
                .GenerateConsensusTransactionsAndMineABlockAsync(stubInitialExtraInformation, testers.Testers[1]);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            // Act
            var consensusTransactions =
                await testers.Testers[1].GenerateConsensusTransactionsAsync(triggerInformationForNormalBlock);

            // Assert
            Assert.NotNull(consensusTransactions);
            Assert.Equal(DPoSBehaviour.UpdateValue.ToString(), consensusTransactions.First().MethodName);
        }

        [Fact]
        public async Task NextRound_GetConsensusCommand()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);
            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, testers.Testers[1]);

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 1).ToTimestamp();
            var command = await testers.Testers[0].GetConsensusCommandAsync(futureTime);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            Assert.True(command.CountingMilliseconds > 0);
            Assert.Equal(4000, command.TimeoutMilliseconds);
        }

        [Fact]
        public async Task NextRound_GetNewConsensusInformation()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, testers.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRoundOrTerm(testers.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var newConsensusInformation =
                await testers.Testers[1].GetNewConsensusInformationAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(2L, newConsensusInformation.Round.RoundNumber);
        }

        [Fact]
        public async Task NextRound_GenerateConsensusTransactions()
        {
            var testers = new ConsensusTesters();
            testers.InitialTesters();

            var triggerInformationForInitialTerm = GetTriggerInformationForInitialTerm(testers.MinersKeyPairs);

            await testers.Testers[0]
                .GenerateConsensusTransactionsAndMineABlockAsync(triggerInformationForInitialTerm, testers.Testers[1]);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRoundOrTerm(testers.Testers[1].KeyPair.PublicKey.ToHex(), futureTime);

            // Act
            var consensusTransactions = await testers.Testers[1]
                .GenerateConsensusTransactionsAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound.ToString(), consensusTransactions.First().MethodName);
        }

        [Fact]
        public async Task NextTerm_GetConsensusCommand()
        {
            const int minersCount = 3;

            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval);

            var miners = Enumerable.Range(0, minersCount)
                .Select(i => starter.CreateNewContractTester(minersKeyPairs[i])).ToList();

            // Produce several blocks.
            await miners.ProduceNormalBlocks(minersCount);

            // Unable to change term.
            {
                var extraBlockMiner = miners.AnyOne();
                var timestamp = DateTime.UtcNow.AddMilliseconds(minersCount * MiningInterval + MiningInterval)
                    .ToTimestamp();
                var command = await extraBlockMiner.GetConsensusCommandAsync(timestamp);
                Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            }

            // Terminate current round then produce several blocks with fake timestamp.
            await miners.ChangeRoundAsync();
            await miners.ProduceNormalBlocks(minersCount,
                DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1).ToTimestamp());

            // Able to changer term.
            {
                var extraBlockMiner = miners.AnyOne();
                var timestamp = DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 2).ToTimestamp();
                var command = await extraBlockMiner.GetConsensusCommandAsync(timestamp);
                Assert.Equal(DPoSBehaviour.NextTerm, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            }
        }

        [Fact]
        public async Task NextTerm_GetNewConsensusInformation_SameMiners()
        {
            const int minersCount = 3;

            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval);

            var miners = Enumerable.Range(0, minersCount)
                .Select(i => starter.CreateNewContractTester(minersKeyPairs[i])).ToList();

            // Produce several blocks.
            await miners.ProduceNormalBlocks(minersCount);

            // Unable to change term.
            {
                var extraBlockMiner = miners.AnyOne();
                var timestamp = DateTime.UtcNow.AddMilliseconds(minersCount * MiningInterval + MiningInterval)
                    .ToTimestamp();
                var triggerInformation = GetTriggerInformationForNextRoundOrTerm(extraBlockMiner.PublicKey, timestamp);
                var consensusInformation = await extraBlockMiner.GetNewConsensusInformationAsync(triggerInformation);
                Assert.Equal(1L, consensusInformation.Round.TermNumber);
            }

            // Terminate current round then produce several blocks with fake timestamp.
            await miners.ChangeRoundAsync();
            await miners.ProduceNormalBlocks(minersCount,
                DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1).ToTimestamp());

            // Able to changer term.
            {
                var extraBlockMiner = miners.AnyOne();
                var timestamp = DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 2).ToTimestamp();
                var triggerInformation = GetTriggerInformationForNextRoundOrTerm(extraBlockMiner.PublicKey, timestamp);
                var consensusInformation = await extraBlockMiner.GetNewConsensusInformationAsync(triggerInformation);
                Assert.Equal(2L, consensusInformation.Round.TermNumber);
            }
        }

        [Fact]
        public async Task NextTerm_GetNewConsensusInformation_NewMiners()
        {
            const int minersCount = 3;

            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs, MiningInterval);

            var initialMiners = Enumerable.Range(0, minersCount)
                .Select(i => starter.CreateNewContractTester(minersKeyPairs[i])).ToList();

            var voter = (await starter.GenerateVotersAsync()).AnyOne();

            var candidates = await starter.GenerateCandidatesAsync(minersCount);

            // Vote to candidates.

            var voteTxs = new List<Transaction>();
            foreach (var candidate in candidates)
            {
                voteTxs.Add(await voter.GenerateTransactionAsync(
                    starter.GetConsensusContractAddress(),
                    nameof(ConsensusContract.Vote),
                    new VoteInput()
                    {
                        CandidatePublicKey = candidate.PublicKey,
                        Amount = 1,
                        LockTime = 100
                    }));
            }

            await initialMiners.MineAsync(voteTxs);

            await initialMiners.RunConsensusAsync(1, true);

            // Check term number.
            {
                var round = await starter.GetCurrentRoundInformationAsync();
                Assert.Equal(2L, round.TermNumber);
            }

            // Current term number is 2. So only if the blockchain age is in range (`DaysEachTerm` * 2, `DaysEachTerm` * 3],
            // can one miner change term to 3rd term.
            await initialMiners.ProduceNormalBlocks(minersCount,
                DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm * 2 + 1).ToTimestamp());

            var extraBlockMiner = initialMiners.AnyOne();
            var timestamp = DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm * 2 + 2).ToTimestamp();
            var triggerInformation = GetTriggerInformationForNextRoundOrTerm(extraBlockMiner.PublicKey, timestamp);
            var consensusInformation = await extraBlockMiner.GetNewConsensusInformationAsync(triggerInformation);

            // Term changed.
            Assert.Equal(3L, consensusInformation.Round.TermNumber);

            // Miners changed to candidates.
            var miners = consensusInformation.Round.RealTimeMinersInformation.Keys.ToList().ToMiners();
            Assert.Equal(candidates.Select(m => m.PublicKey).ToList().ToMiners().GetMinersHash(),
                miners.GetMinersHash());
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
                previousInValue = Hash.Empty;
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

        public List<ContractTester<DPoSContractTestAElfModule>> Testers { get; set; } =
            new List<ContractTester<DPoSContractTestAElfModule>>();

        public ContractTester<DPoSContractTestAElfModule> SingleTester { get; set; }

        public Address ConsensusContractAddress { get; set; }

        public void InitialTesters()
        {
            for (var i = 0; i < MinersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                MinersKeyPairs.Add(keyPair);
                var tester = new ContractTester<DPoSContractTestAElfModule>(ChainId, keyPair);

                AsyncHelper.RunSync(
                    () => tester.InitialChainAsync());
                Testers.Add(tester);
            }

            ConsensusContractAddress = Testers[0].GetConsensusContractAddress();
        }

        public void InitialSingleTester()
        {
            SingleTester = new ContractTester<DPoSContractTestAElfModule>(ChainId, CryptoHelpers.GenerateKeyPair());
            AsyncHelper.RunSync(
                () => SingleTester.InitialChainAsync());
            ConsensusContractAddress = SingleTester.GetConsensusContractAddress();
        }
    }
}