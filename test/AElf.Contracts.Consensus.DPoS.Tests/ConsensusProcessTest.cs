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
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    // TODO: Rewrite via test kit.
    public class ConsensusProcessTest : ContractTestBase<DPoSContractTestAElfModule>
    {
        private const int MiningInterval = 4000;

        [Fact(Skip = "Rewrite")]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation =
                await testers.Testers[1].GetInformationToUpdateConsensusAsync(stubExtraInformation, DateTime.UtcNow);

            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(outValue, newConsensusInformation.Round
                .RealTimeMinersInformation[testers.Testers[1].KeyPair.PublicKey.ToHex()]
                .OutValue);
        }

        [Fact(Skip = "Rewrite")]
        public async Task NormalBlock_ValidationConsensus_Success()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            var newInformation =
                await testers.Testers[1].GetInformationToUpdateConsensusAsync(triggerInformationForNormalBlock, DateTime.UtcNow);

            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);

            // Assert
            Assert.True(validationResult?.Success);
        }

        [Fact(Skip = "Rewrite")]
        public async Task NormalBlock_GenerateConsensusTransactions()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);
            
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

        [Fact(Skip = "Rewrite")]
        public async Task NextRound_GetConsensusCommand()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 1).ToTimestamp();
            var command = await testers.Testers[0].GetConsensusCommandAsync(futureTime);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound, DPoSHint.Parser.ParseFrom(command.Hint).Behaviour);
            Assert.True(command.NextBlockMiningLeftMilliseconds > 0);
            Assert.Equal(4000, command.LimitMillisecondsOfMiningBlock);
        }

        [Fact(Skip = "Rewrite")]
        public async Task NextRound_GetNewConsensusInformation()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRound(testers.Testers[1].KeyPair.PublicKey.ToHex());

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000);
            var newConsensusInformation =
                await testers.Testers[1].GetInformationToUpdateConsensusAsync(triggerInformationForNextRoundOrTerm, futureTime);

            // Assert
            Assert.Equal(2L, newConsensusInformation.Round.RoundNumber);
        }

        [Fact(Skip = "Rewrite")]
        public async Task NextRound_GenerateConsensusTransactions()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount + 4000).ToTimestamp();
            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRound(testers.Testers[1].KeyPair.PublicKey.ToHex());

            // Act
            var consensusTransactions = await testers.Testers[1]
                .GenerateConsensusTransactionsAsync(triggerInformationForNextRoundOrTerm);

            // Assert
            Assert.Equal(DPoSBehaviour.NextRound.ToString(), consensusTransactions.First().MethodName);
        }

        [Fact(Skip = "Rewrite")]
        public async Task NextTerm_GetConsensusCommand()
        {
            const int minersCount = 3;

            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs);

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

            // Terminate current round then produce several blocks with fake timestamp.
            await miners.ChangeRoundAsync();
            await miners.ProduceNormalBlocks(minersCount,
                DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1).ToTimestamp());

            // Able to changer term.
            {
                var extraBlockMiner = miners.AnyOne();
                var timestamp = DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 2);
                var triggerInformation = GetTriggerInformationForNextTerm(extraBlockMiner.PublicKey);
                var consensusInformation = await extraBlockMiner.GetInformationToUpdateConsensusAsync(triggerInformation, timestamp);
                Assert.Equal(2L, consensusInformation.Round.TermNumber);
            }
        }

        [Fact]
        public async Task NextTerm_GetNewConsensusInformation_NewMiners()
        {
            const int minersCount = 3;

            var starter = new ContractTester<DPoSContractTestAElfModule>();

            var minersKeyPairs = Enumerable.Range(0, minersCount).Select(_ => CryptoHelpers.GenerateKeyPair()).ToList();
            await starter.InitialChainAndTokenAsync(minersKeyPairs);

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
            var timestamp = DateTime.UtcNow.AddMinutes(ConsensusDPoSConsts.DaysEachTerm * 2 + 2);
            var triggerInformation = GetTriggerInformationForNextTerm(extraBlockMiner.PublicKey);
            var consensusInformation =
                await extraBlockMiner.GetInformationToUpdateConsensusAsync(triggerInformation, timestamp);

            // Term changed.
            Assert.Equal(3L, consensusInformation.Round.TermNumber);

            // Miners changed to candidates.
            var miners = consensusInformation.Round.RealTimeMinersInformation.Keys.ToList().ToMiners();
            Assert.Equal(candidates.Select(m => m.PublicKey).ToList().ToMiners().GetMinersHash(),
                miners.GetMinersHash());
        }

        [Fact]
        public async Task Set_ConfigStrategy()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var input = new DPoSStrategyInput
            {
                IsBlockchainAgeSettable = true,
                IsTimeSlotSkippable = true,
                IsVerbose = true
            };

            var transactionResult = await testers.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //set again
            transactionResult = await testers.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already configured").ShouldBeTrue();
        }
        
        private DPoSTriggerInformation GetTriggerInformationForNormalBlock(string publicKey, Hash randomHash,
            Hash previousRandomHash = null)
        {
            if (previousRandomHash == null)
            {
                previousRandomHash = Hash.Empty;
            }

            return new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                PreviousRandomHash = previousRandomHash,
                RandomHash = randomHash,
                Behaviour = DPoSBehaviour.UpdateValue
            };
        }

        private DPoSTriggerInformation GetTriggerInformationForNextRound(string publicKey)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                Behaviour = DPoSBehaviour.NextRound
            };
        }
        
        private DPoSTriggerInformation GetTriggerInformationForNextTerm(string publicKey)
        {
            return new DPoSTriggerInformation
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey)),
                Behaviour = DPoSBehaviour.NextTerm
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

        public void InitialTesters(Timestamp blockchainStartTime)
        {
            for (var i = 0; i < MinersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                MinersKeyPairs.Add(keyPair);
            }

            foreach (var minersKeyPair in MinersKeyPairs)
            {
                var tester = new ContractTester<DPoSContractTestAElfModule>(ChainId, minersKeyPair);
                AsyncHelper.RunSync(() =>
                    tester.InitialCustomizedChainAsync(MinersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList(), 4000,
                        blockchainStartTime));
                Testers.Add(tester);
            }

            AsyncHelper.RunSync(() => Testers.RunConsensusAsync(2));

            ConsensusContractAddress = Testers[0].GetConsensusContractAddress();
        }
    }
}