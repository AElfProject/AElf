using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
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
        public async Task Validation_ConsensusBeforeExecution_Success()
        {
            TesterManager.InitialTesters();
            
            //invalid dpos header info 
            {
                var invalidInput = new DPoSHeaderInformation
                {
                    SenderPublicKey = ByteString.CopyFrom(TesterManager.Testers[0].KeyPair.PublicKey)
                };
            
                var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(invalidInput);
                validationResult.Success.ShouldBeFalse();
            }
            
            await InitialConsensus_Success();
            var roundInfo = await TesterManager.Testers[0].GetCurrentRoundInformation();
            var input = new DPoSHeaderInformation
            {
                SenderPublicKey = ByteString.CopyFrom(TesterManager.Testers[0].KeyPair.PublicKey),
                Round = roundInfo,
                Behaviour = DPoSBehaviour.Nothing
            };
            
            //invalid behavior
            {
                var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(input);
                validationResult.Success.ShouldBeFalse();
                validationResult.Message.ShouldBe("Invalid behaviour");
            }

            //invalid out value
            {
                input.Behaviour = DPoSBehaviour.UpdateValue;
                var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(input);
                validationResult.Success.ShouldBeFalse();
                validationResult.Message.ShouldBe("Incorrect new Out Value.");
            }
            
            //valid data
            {
                input.Behaviour = DPoSBehaviour.NextRound;
                var validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(input);
                validationResult.Success.ShouldBeTrue();

                input.Behaviour = DPoSBehaviour.NextTerm;
                validationResult = await TesterManager.Testers[0].ValidateConsensusBeforeExecutionAsync(input);
                validationResult.Success.ShouldBeTrue();
            }
        }
        
        [Fact]
        public async Task Validation_ConsensusAfterExecution_Success()
        {
            TesterManager.InitialTesters();

            var dposInformation = new DPoSHeaderInformation();

            var validationResult = await TesterManager.Testers[0].ValidateConsensusAfterExecutionAsync(dposInformation);
            validationResult.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task Get_ConsensusCommand_Success()
        {
            TesterManager.InitialTesters();

            var consensusCommand = await TesterManager.Testers[0].GetConsensusCommandAsync();
            consensusCommand.ShouldNotBeNull();

            var behavior = DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour;
            behavior.ShouldBe(DPoSBehaviour.Nothing);
        }

        [Fact]
        public async Task InitialConsensus_Failed()
        {
            TesterManager.InitialTesters();

            //invalid round number
            {
                var roundInformation = new Round
                {
                    RoundNumber = 2
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.InitialConsensus), roundInformation);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid round number").ShouldBeTrue();
            }

            //invalid miners info
            {
                var roundInformation = new Round
                {
                    RoundNumber = 1,
                    RealTimeMinersInformation = { }
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.InitialConsensus), roundInformation);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("No miner in input data").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task InitialConsensus_Success()
        {
            TesterManager.InitialTesters();

            var roundInformation = new Round
            {
                RoundNumber = 1,
                RealTimeMinersInformation =
                {
                    {
                        TesterManager.Testers[0].PublicKey, new MinerInRound
                        {
                            Alias = "first-bp",
                            Order = 1,
                            ExpectedMiningTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)).ToTimestamp(),
                        }
                    }
                }
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.InitialConsensus), roundInformation);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var currentRound = await TesterManager.Testers[0].GetCurrentRoundInformation();
            currentRound.RoundNumber.ShouldBe(1);
        }

        [Fact]
        public async Task Set_ConfigStrategy()
        {
            TesterManager.InitialTesters();

            var input = new DPoSStrategyInput
            {
                IsBlockchainAgeSettable = true,
                IsTimeSlotSkippable = true,
                IsVerbose = true
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //set again
            transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.ConfigStrategy), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already configured").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateValue_Failed()
        {
            TesterManager.InitialTesters();

            //invalid round number
            var input = new ToUpdate
            {
                RoundId = 1234
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.UpdateValue), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Round information not found").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateValue_Success()
        {
            TesterManager.InitialTesters();

            await InitialConsensus_Success();
            var roundInfo = await TesterManager.Testers[0].GetCurrentRoundInformation();

            var input = new ToUpdate
            {
                RoundId = roundInfo.RoundId,
                Signature = Hash.Generate(),
                OutValue = Hash.Generate(),
                ProducedBlocks = 1,
                ActualMiningTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4)).ToTimestamp(),
                SupposedOrderOfNextRound = 1,
                PromiseTinyBlocks = 2,
                PreviousInValue = Hash.Generate(),
                MinersPreviousInValues =
                {
                    {TesterManager.Testers[0].PublicKey, Hash.Generate()}
                }
            };

            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.UpdateValue), input);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task NextRound_Failed()
        {
            TesterManager.InitialTesters();
            await InitialConsensus_Success();
            
            var input = new Round
            {
                RoundNumber = 1,
                BlockchainAge = 10
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Incorrect round number for next round.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task NextRound_Success()
        {
            TesterManager.InitialTesters();
            await InitialConsensus_Success();
            
            var input = new Round
            {
                RoundNumber = 2,
                BlockchainAge = 10,
                ExtraBlockProducerOfPreviousRound = TesterManager.Testers[1].PublicKey
            };
            var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                nameof(ConsensusContract.NextRound), input);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task GetInformationToUpdateConsensus_Failed()
        {
            TesterManager.InitialTesters();

            //invalid public key
            {
                var input = new DPoSTriggerInformation
                {
                    RandomHash = Hash.Generate(),
                    Behaviour = DPoSBehaviour.NextRound
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.GetInformationToUpdateConsensus), input);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid public key").ShouldBeTrue();
            }
            
            //invalid round info
            {
                var input = new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.UpdateValue,
                    PublicKey = ByteString.CopyFromUtf8(TesterManager.Testers[0].PublicKey),
                };
                
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.GetInformationToUpdateConsensus), input);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Failed to get current round information").ShouldBeTrue();
            }

            //valid data but random public key
            {
                var input = new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.UpdateValue,
                    InitialTermNumber = 2,
                    PreviousRandomHash = Hash.Generate(),
                    PublicKey = ByteString.CopyFrom(CryptoHelpers.GenerateKeyPair().PublicKey),
                    RandomHash = Hash.Generate()
                };

                await InitialConsensus_Success();
                
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.GetInformationToUpdateConsensus), input);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("The given key was not present in the dictionary").ShouldBeTrue();
            }
        }
        
        [Fact]
        public async Task GenerateConsensusTransactions_Failed()
        {
            TesterManager.InitialTesters();
            
            //without public key
            {
                var input = new DPoSTriggerInformation
                {
                    Behaviour = DPoSBehaviour.NextRound,
                    InitialTermNumber = 1
                };
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.GenerateConsensusTransactions), input);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Data to request consensus information should contain public key").ShouldBeTrue();
            }
            
            //with random public key
            {
                var input = new DPoSTriggerInformation
                {
                    PublicKey = ByteString.CopyFrom(CryptoHelpers.GenerateKeyPair().PublicKey),
                    Behaviour = DPoSBehaviour.NextRound,
                    RandomHash = Hash.Generate()
                };

                await InitialConsensus_Success();
                
                var transactionResult = await TesterManager.Testers[0].ExecuteConsensusContractMethodWithMiningAsync(
                    nameof(ConsensusContract.GenerateConsensusTransactions), input);
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("The given key was not present in the dictionary").ShouldBeTrue();
            }
        }
    }
}