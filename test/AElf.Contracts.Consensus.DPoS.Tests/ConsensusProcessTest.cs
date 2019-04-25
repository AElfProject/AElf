using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Consensus.DPoS;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
//using VoteInput = AElf.Consensus.DPoS.VoteInput;

namespace AElf.Contracts.Consensus.DPoS
{
    public class ConsensusProcessTest : ContractTestBase<DPoSContractTestAElfModule>
    {
        private const int MiningInterval = 4000;

        [Fact]
        public async Task NormalBlock_GetNewConsensusInformation()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var inValue = Hash.Generate();
            var stubExtraInformation =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            // Act
            var newConsensusInformation =
                await testers.Testers[1].GetInformationToUpdateConsensusAsync(stubExtraInformation, DateTime.UtcNow);

            // Assert
            Assert.NotNull(newConsensusInformation);
            Assert.Equal(testers.Testers[1].PublicKey, newConsensusInformation.SenderPublicKey.ToHex());
        }
        
        [Fact]
        public async Task NormalBlock_ValidateConsensusBeforeExecution_Failed()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var newInformation = new DPoSHeaderInformation
            {
                SenderPublicKey = ByteString.CopyFrom(testers.Testers[0].KeyPair.PublicKey),
                Round = await testers.Testers[0].GetCurrentRoundInformationAsync(),
                Behaviour = DPoSBehaviour.UpdateValueWithoutPreviousInValue
            };
            
            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);
            validationResult.Success.ShouldBeFalse();
            
            newInformation.Round.RealTimeMinersInformation.First().Value.OutValue = Hash.Generate();
            validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);
            validationResult.Success.ShouldBeFalse();
        }
        
        [Fact]
        public async Task NormalBlock_ValidateConsensusBeforeExecution_Success()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);
            var currentRound = await testers.Testers[0].GetCurrentRoundInformationAsync();
            var roundInfo = new Round
            {
                BlockchainAge = currentRound.BlockchainAge + 1,
                RoundNumber = currentRound.RoundNumber + 1,
                TermNumber = currentRound.TermNumber,
                RealTimeMinersInformation =
                {
                    { testers.Testers[0].PublicKey, new MinerInRound
                        {
                            OutValue = Hash.Generate(),
                            FinalOrderOfNextRound = 1,
                            ExpectedMiningTime = currentRound.RealTimeMinersInformation[testers.Testers[0].PublicKey].ExpectedMiningTime,
                            Order = 1
                            
                        }
                    },
                    {
                        testers.Testers[1].PublicKey, new MinerInRound
                        {
                            OutValue = Hash.Generate(),
                            FinalOrderOfNextRound = 2,
                            ExpectedMiningTime = currentRound.RealTimeMinersInformation[testers.Testers[1].PublicKey].ExpectedMiningTime,
                            Order = 2
                        }
                    },
                    {
                        testers.Testers[2].PublicKey, new MinerInRound
                        {
                            OutValue = Hash.Generate(),
                            FinalOrderOfNextRound = 3,
                            ExpectedMiningTime = currentRound.RealTimeMinersInformation[testers.Testers[2].PublicKey].ExpectedMiningTime,
                            Order = 3
                        }
                    },
                }
            }; 
                 
            var newInformation = new DPoSHeaderInformation
            {
                SenderPublicKey = ByteString.CopyFrom(testers.Testers[0].KeyPair.PublicKey),
                Round = roundInfo,
                Behaviour = DPoSBehaviour.NextRound
            };
            
            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);
            validationResult.Success.ShouldBeTrue();

            //nothing behavior
            newInformation.Behaviour = DPoSBehaviour.Nothing;
            validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);
            validationResult.Success.ShouldBeFalse();
            validationResult.Message.ShouldBe("Invalid behaviour");
            
            //update value
            newInformation.Behaviour = DPoSBehaviour.UpdateValue;
            validationResult = await testers.Testers[0].ValidateConsensusBeforeExecutionAsync(newInformation);
            validationResult.ShouldNotBeNull();
        }

        [Fact]
        public async Task NormalBlock_ValidateConsensusAfterExecution_Failed()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var inValue = Hash.Generate();
            var triggerInformationForNormalBlock =
                GetTriggerInformationForNormalBlock(testers.Testers[1].KeyPair.PublicKey.ToHex(), inValue);

            var newInformation =
                await testers.Testers[1]
                    .GetInformationToUpdateConsensusAsync(triggerInformationForNormalBlock, DateTime.UtcNow);
            
            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusAfterExecutionAsync(newInformation);
            validationResult.Success.ShouldBeFalse();
            validationResult.Message.ShouldBe("Current round information is different with consensus extra data.");
        }
        
        [Fact]
        public async Task NormalBlock_ValidateConsensusAfterExecution_Success()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var newInformation = new DPoSHeaderInformation
            {
                SenderPublicKey = ByteString.CopyFrom(testers.Testers[0].KeyPair.PublicKey),
                Round = await testers.Testers[0].GetCurrentRoundInformationAsync(),
                Behaviour = DPoSBehaviour.UpdateValueWithoutPreviousInValue
            };
            
            // Act
            var validationResult = await testers.Testers[0].ValidateConsensusAfterExecutionAsync(newInformation);
            validationResult.Success.ShouldBeTrue();
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task NextRound_GetNewConsensusInformation()
        {
            var startTime = DateTime.UtcNow.ToTimestamp();
            var testers = new ConsensusTesters();
            testers.InitialTesters(startTime);

            var triggerInformationForNextRoundOrTerm =
                GetTriggerInformationForNextRound(testers.Testers[1].KeyPair.PublicKey.ToHex());

            // Act
            var futureTime = DateTime.UtcNow.AddMilliseconds(4000 * testers.MinersCount);
            var newConsensusInformation =
                await testers.Testers[1].GetInformationToUpdateConsensusAsync(triggerInformationForNextRoundOrTerm, futureTime);

            // Assert
            newConsensusInformation.SenderPublicKey.ToHex().ShouldBe(testers.Testers[1].PublicKey);
            newConsensusInformation.Round.RoundNumber.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
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
                    new VoteInput
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