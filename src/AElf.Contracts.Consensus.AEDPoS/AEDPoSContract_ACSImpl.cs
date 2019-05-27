using System;
using System.Linq;
using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// In this method, `Context.CurrentBlockTime` is the time one miner start request his next consensus command.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override ConsensusCommand GetConsensusCommand(BytesValue input)
        {
            // Query state to determine whether produce tiny block.
            Assert(input.Value.Any(), "Invalid public key.");

            TryToGetCurrentRoundInformation(out var currentRound);

            if (currentRound == null || currentRound.RoundId == 0)
            {
                return new ConsensusCommand
                {
                    ExpectedMiningTime = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    LimitMillisecondsOfMiningBlock = int.MaxValue, NextBlockMiningLeftMilliseconds = int.MaxValue
                };
            }

            var behaviour = GetBehaviour(currentRound, input.Value.ToHex());

            if (behaviour == AElfConsensusBehaviour.Nothing)
            {
                return new ConsensusCommand
                {
                    ExpectedMiningTime = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Hint = ByteString.CopyFrom(new AElfConsensusHint {Behaviour = behaviour}.ToByteArray()),
                    LimitMillisecondsOfMiningBlock = int.MaxValue, NextBlockMiningLeftMilliseconds = int.MaxValue
                };
            }

            var command = GetConsensusCommand(behaviour, currentRound, input.Value.ToHex());

            Context.LogDebug(() =>
                currentRound.GetLogs(input.Value.ToHex(), AElfConsensusHint.Parser.ParseFrom(command.Hint).Behaviour));

            return command;
        }

        public override BytesValue GetInformationToUpdateConsensus(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);

            Assert(triggerInformation.PublicKey.Any(), "Invalid public key.");

            if (!TryToGetCurrentRoundInformation(out var currentRound))
            {
                Assert(false, "Failed to get current round information.");
            }

            var publicKeyBytes = triggerInformation.PublicKey;
            var publicKey = publicKeyBytes.ToHex();
            
            switch (triggerInformation.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return GetInformationToUpdateConsensusToPublishOutValue(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.TinyBlock:
                    return GetInformationToUpdateConsensusForTinyBlock(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.NextRound:
                    return GetInformationToUpdateConsensusForNextRound(currentRound, publicKey,
                        triggerInformation).ToBytesValue();
                case AElfConsensusBehaviour.NextTerm:
                    return GetInformationToUpdateConsensusForNextTerm(publicKey, triggerInformation)
                        .ToBytesValue();
                default:
                    return new BytesValue();
            }
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input)
        {
            var triggerInformation = new AElfConsensusTriggerInformation();
            triggerInformation.MergeFrom(input.Value);
            // Some basic checks.
            Assert(triggerInformation.PublicKey.Any(),
                "Data to request consensus information should contain public key.");

            var publicKey = triggerInformation.PublicKey;
            var consensusInformation = new AElfConsensusHeaderInformation();
            consensusInformation.MergeFrom(GetInformationToUpdateConsensus(input).Value);
            var round = consensusInformation.Round;
            var behaviour = consensusInformation.Behaviour;
            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(publicKey.ToHex()))
                        }
                    };
                case AElfConsensusBehaviour.TinyBlock:
                    var minerInRound = round.RealTimeMinersInformation[publicKey.ToHex()];
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateTinyBlockInformation),
                                new TinyBlockInput
                                {
                                    ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
                                    ProducedBlocks = minerInRound.ProducedBlocks,
                                    RoundId = round.RoundId
                                })
                        }
                    };
                case AElfConsensusBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound), round)
                        }
                    };
                case AElfConsensusBehaviour.NextTerm:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextTerm), round)
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input)
        {
            var extraData = AElfConsensusHeaderInformation.Parser.ParseFrom(input.Value.ToByteArray());
            var publicKey = extraData.SenderPublicKey;

            // Validate the sender.
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey.ToHex()))
            {
                return new ValidationResult {Success = false, Message = $"Sender {publicKey.ToHex()} is not a miner."};
            }

            // Validate the time slots.
            var timeSlotsCheckResult = extraData.Round.CheckTimeSlots();
            if (!timeSlotsCheckResult.Success)
            {
                return timeSlotsCheckResult;
            }

            var behaviour = extraData.Behaviour;

            // Try to get current round information (for further validation).
            if (currentRound == null)
            {
                return new ValidationResult
                    {Success = false, Message = "Failed to get current round information."};
            }

            if (extraData.Round.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() !=
                extraData.Round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                return new ValidationResult
                    {Success = false, Message = "Invalid FinalOrderOfNextRound."};
            }

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    // Need to check round id when updating current round information.
                    // This can tell the miner current block 
                    if (!RoundIdMatched(extraData.Round))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    // Only one Out Value should be filled.
                    if (!NewOutValueFilled(extraData.Round.RealTimeMinersInformation.Values))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }

                    break;
                case AElfConsensusBehaviour.NextRound:
                    // None of in values should be filled.
                    if (extraData.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect in values."};
                    }

                    break;
                case AElfConsensusBehaviour.NextTerm:
                    break;
                case AElfConsensusBehaviour.Nothing:
                    return new ValidationResult {Success = false, Message = "Invalid behaviour"};
                case AElfConsensusBehaviour.TinyBlock:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ValidationResult {Success = true};
        }

        public override ValidationResult ValidateConsensusAfterExecution(BytesValue input1)
        {
            var input = new AElfConsensusHeaderInformation();
            input.MergeFrom(input1.Value);
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var isContainPreviousInValue =
                    input.Behaviour != AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue;
                if (input.Round.GetHash(isContainPreviousInValue) != currentRound.GetHash(isContainPreviousInValue))
                {
                    Context.LogDebug(() => $"Round information of block header:\n{input.Round}");
                    Context.LogDebug(() => $"Round information of executing result:\n{currentRound}");
                    return new ValidationResult
                    {
                        Success = false, Message = "Current round information is different with consensus extra data."
                    };
                }
            }

            return new ValidationResult {Success = true};
        }
    }
}