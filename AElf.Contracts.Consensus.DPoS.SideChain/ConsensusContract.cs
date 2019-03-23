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

        public override ConsensusCommand GetConsensusCommand(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Trigger information should contain public key.");

            var publicKey = input.PublicKey.ToHex();
            var currentBlockTime = Context.CurrentBlockTime;

            Context.LogDebug(() => GetLogStringForOneRound(publicKey));

            var behaviour = GetBehaviour(publicKey, currentBlockTime, out var round, out var minerInRound);

            TryToGetMiningInterval(out var miningInterval);
            return behaviour.GetConsensusCommand(round, minerInRound, miningInterval, currentBlockTime,
                input.IsBootMiner);
        }

        public override DPoSHeaderInformation GetInformationToUpdateConsensus(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");

            var publicKey = input.PublicKey;
            var currentBlockTime = Context.CurrentBlockTime;

            var behaviour = GetBehaviour(publicKey.ToHex(), currentBlockTime, out var round, out _);

            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    var miningInterval = input.MiningInterval;
                    var initialMiners = input.Miners;
                    var firstRound = initialMiners.ToList().ToMiners(1)
                        .GenerateFirstRoundOfNewTerm(miningInterval);
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = firstRound,
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    Assert(input.CurrentInValue != null && input.CurrentInValue != null,
                        "Current in value should be valid.");

                    var previousInValue = input.PreviousInValue;

                    var inValue = input.CurrentInValue;

                    var outValue = Hash.FromMessage(inValue);

                    var signature = Hash.Empty;
                    if (round.RoundNumber != 1)
                    {
                        Assert(TryToGetPreviousRoundInformation(out var previousRound),
                            "Failed to get previous round information.");
                        signature = previousRound.CalculateSignature(inValue);
                    }

                    var updatedRound = round.ApplyNormalConsensusData(publicKey.ToHex(), previousInValue, outValue,
                        signature,
                        currentBlockTime);
                    // To publish Out Value.
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.NextRound:
                    Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp));
                    Assert(
                        GenerateNextRoundInformation(round, currentBlockTime, blockchainStartTimestamp,
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
                case DPoSBehaviour.Invalid:
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
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
                case DPoSBehaviour.InitialConsensus:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(InitialConsensus), round)
                        }
                    };
                case DPoSBehaviour.UpdateValueWithoutPreviousInValue:
                case DPoSBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                round.GenerateInformationToUpdateConsensus(publicKey.ToHex()))
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
                case DPoSBehaviour.Invalid:
                    return new TransactionList();
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
            if (currentRound == null && behaviour != DPoSBehaviour.InitialConsensus)
            {
                return new ValidationResult
                    {Success = false, Message = "Failed to get current round information."};
            }

            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    break;
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
                case DPoSBehaviour.Invalid:
                    return new ValidationResult {Success = false, Message = "Invalid behaviour."};
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ValidationResult {Success = true};
        }

        public override ValidationResult ValidateConsensusAfterExecution(DPoSHeaderInformation input)
        {
            if (input.Behaviour != DPoSBehaviour.InitialConsensus &&
                TryToGetCurrentRoundInformation(out var currentRound))
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

        /// <summary>
        /// Get next consensus behaviour of the caller based on current state.
        /// This method can be tested by testing GetConsensusCommand.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="dateTime"></param>
        /// <param name="round"></param>
        /// <param name="minerInRound"></param>
        /// <returns></returns>
        private DPoSBehaviour GetBehaviour(string publicKey, DateTime dateTime, out Round round,
            out MinerInRound minerInRound)
        {
            round = null;
            minerInRound = null;

            // If we can't get current round information from state db, it means this chain hasn't initialized yet,
            // so the context of current command is to initial a new chain via creating the consensus initial information.
            // And to initial DPoS information, we need to generate the information of first round, at least.
            if (!TryToGetCurrentRoundInformation(out round))
            {
                return DPoSBehaviour.InitialConsensus;
            }

            if (!round.IsTimeSlotPassed(publicKey, dateTime, out minerInRound) && minerInRound.OutValue == null)
            {
                return minerInRound != null
                    ? (round.RoundNumber == 1
                        ? DPoSBehaviour.UpdateValueWithoutPreviousInValue
                        : DPoSBehaviour.UpdateValue)
                    : DPoSBehaviour.Invalid;
            }

            // If this node missed his time slot, a command of terminating current round will be fired,
            // and the terminate time will based on the order of this node (to avoid conflicts).

            // Calculate the approvals and make the judgement of changing term.
            Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                "Failed to get blockchain start timestamp.");
            Assert(TryToGetTermNumber(out var termNumber), "Failed to get term number.");
            if (round.RoundNumber == 1)
            {
                return DPoSBehaviour.NextRound;
            }

            Assert(TryToGetPreviousRoundInformation(out var previousRound), "Failed to previous round information.");
            return round.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp.ToDateTime(), termNumber)
                ? DPoSBehaviour.NextTerm
                : DPoSBehaviour.NextRound;
        }

        private string GetLogStringForOneRound(string publicKey)
        {
            if (!TryToGetCurrentRoundInformation(out var round))
            {
                return "";
            }

            var logs = new StringBuilder($"\n[Round {round.RoundNumber}](Round Id: {round.RoundId})");
            foreach (var minerInRound in round.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = new StringBuilder("\n");
                minerInformation.Append($"[{minerInRound.PublicKey.Substring(0, 10)}]");
                minerInformation.Append(minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "");
                minerInformation.Append(minerInRound.PublicKey == publicKey
                    ? "(This Node)"
                    : "");
                minerInformation.AppendLine($"Order:\t {minerInRound.Order}");
                minerInformation.AppendLine(
                    $"Time:\t {minerInRound.ExpectedMiningTime.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,fff}");
                minerInformation.AppendLine($"Out:\t {minerInRound.OutValue?.ToHex()}");
                if (round.RoundNumber != 1)
                {
                    minerInformation.AppendLine($"\nPreIn:\t {minerInRound.PreviousInValue?.ToHex()}");
                }

                minerInformation.AppendLine($"\nSig:\t {minerInRound.Signature?.ToHex()}");
                minerInformation.AppendLine($"\nMine:\t {minerInRound.ProducedBlocks}");
                minerInformation.AppendLine($"\nMiss:\t {minerInRound.MissedTimeSlots}");
                minerInformation.AppendLine($"\nProms:\t {minerInRound.PromisedTinyBlocks}");
                minerInformation.AppendLine($"\nNOrder:\t {minerInRound.OrderOfNextRound}");

                logs.Append(minerInformation);
            }

            return logs.ToString();
        }
    }
}