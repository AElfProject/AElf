using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable UnusedMember.Global
    public partial class ConsensusContract : ConsensusContractContainer.ConsensusContractBase
    {
        // This file contains implementations of IConsensusSmartContract.

        public override ConsensusCommand GetConsensusCommand(CommandInput input)
        {
            Assert(input.PublicKey.Any(), "Invalid public key.");
            var behaviour = GetBehaviour(input.PublicKey.ToHex(), Context.CurrentBlockTime, out var currentRound);
            Context.LogDebug(() => currentRound.GetLogs(input.PublicKey.ToHex(), behaviour));
            return behaviour.GetConsensusCommand(currentRound, input.PublicKey.ToHex(), Context.CurrentBlockTime);
        }

        public override DPoSHeaderInformation GetInformationToUpdateConsensus(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var publicKey = input.PublicKey;
            var currentBlockTime = Context.CurrentBlockTime;
            var behaviour = GetBehaviour(publicKey.ToHex(), currentBlockTime, out var currentRound);
            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    Assert(input.RandomHash != null, "Random hash should not be null.");

                    var inValue = currentRound.CalculateInValue(input.RandomHash);
                    var outValue = Hash.FromMessage(inValue);
                    var signature = Hash.FromTwoHashes(outValue, input.RandomHash);// Just initial signature value.
                    var previousInValue = Hash.Empty;// Just initial previous in value.

                    TryToGetPreviousRoundInformation(out var previousRound);
                    if (previousRound.RoundId != 0 && previousRound.TermNumber == currentRound.TermNumber)
                    {
                        signature = previousRound.CalculateSignature(inValue);
                        previousInValue = previousRound.CalculateInValue(input.PreviousRandomHash);
                    }

                    var updatedRound = currentRound.ApplyNormalConsensusData(publicKey.ToHex(), previousInValue, outValue, signature,
                        currentBlockTime);
                    // To publish Out Value.
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour,
                    };
                case DPoSBehaviour.NextRound:
                    Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp));
                    Assert(
                        GenerateNextRoundInformation(currentRound, currentBlockTime, blockchainStartTimestamp,
                            out var nextRound),
                        "Failed to generate next round information.");
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.NextTerm:
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = GenerateFirstRoundOfNextTerm(),
                        Behaviour = behaviour
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override TransactionList GenerateConsensusTransactions(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");

            var publicKey = input.PublicKey;

            var consensusInformation = GetInformationToUpdateConsensus(input);

            var round = consensusInformation.Round;

            var behaviour = consensusInformation.Behaviour;

            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.ExtractInformationToUpdateConsensus(publicKey.ToHex()))
                        }
                    };
                case DPoSBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound), round)
                        }
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ValidationResult ValidateConsensusBeforeExecution(DPoSHeaderInformation input)
        {
            var publicKey = input.SenderPublicKey;

            // Validate the sender.
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey.ToHex()))
            {
                return new ValidationResult {Success = false, Message = "Sender is not a miner."};
            }

            // Validate the time slots.
            var timeSlotsCheckResult = input.Round.CheckTimeSlots();
            if (!timeSlotsCheckResult.Success)
            {
                return timeSlotsCheckResult;
            }

            var behaviour = input.Behaviour;

            // Try to get current round information (for further validation).
            if (currentRound == null)
            {
                return new ValidationResult
                    {Success = false, Message = "Failed to get current round information."};
            }

            switch (behaviour)
            {
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    // Need to check round id when updating current round information.
                    // This can tell the miner current block 
                    if (!RoundIdMatched(input.Round))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    // Only one Out Value should be filled.
                    // TODO: Miner can only update his information.
                    if (!NewOutValueFilled(input.Round.RealTimeMinersInformation.Values))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }

                    break;
                case DPoSBehaviour.NextRound:
                    // None of in values should be filled.
                    if (!InValueIsNull(input.Round))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect in values."};
                    }

                    break;
                case DPoSBehaviour.NextTerm:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ValidationResult {Success = true};
        }

        public override ValidationResult ValidateConsensusAfterExecution(DPoSHeaderInformation input)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var isContainPreviousInValue = input.Behaviour != DPoSBehaviour.UpdateValueWithoutPreviousInValue;
                if (input.Round.GetHash(isContainPreviousInValue) != currentRound.GetHash(isContainPreviousInValue))
                {
                    return new ValidationResult {Success = false, Message = "Invalid round information."};
                }
            }

            // TODO: Still need to check: ProducedBlocks, //

            return new ValidationResult {Success = true};
        }

        
    }
}