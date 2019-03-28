using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable UnusedMember.Global
    public partial class ConsensusContract : ConsensusContractContainer.ConsensusContractBase
    {
        // This file contains implementations of IConsensusSmartContract.

        public override ConsensusCommand GetConsensusCommand(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Trigger information should contain public key.");
            Assert(input.Timestamp != null, "Trigger information should contain timestamp.");

            var publicKey = input.PublicKey;
            var timestamp = input.Timestamp;

            Context.LogDebug(() => GetLogStringForOneRound(publicKey));

            var behaviour = GetBehaviour(publicKey, timestamp, out var round, out var minerInRound);

            TryToGetMiningInterval(out var miningInterval);
            return behaviour.GetConsensusCommand(round, minerInRound, miningInterval, timestamp, input.IsBootMiner);
        }

        public override DPoSInformation GetNewConsensusInformation(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");
            Assert(input.Timestamp != null, "Data to request consensus information should contain timestamp.");

            var publicKey = input.PublicKey;
            var timestamp = input.Timestamp;

            var behaviour = GetBehaviour(publicKey, timestamp, out var round, out _);

            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    var miningInterval = input.MiningInterval;
                    var initialMiners = input.Miners;
                    var firstRound = initialMiners.ToList().ToMiners(1).GenerateFirstRoundOfNewTerm(miningInterval);
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = firstRound,
                        Behaviour = behaviour
                    };
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

                    // To publish Out Value.
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = round.ApplyNormalConsensusData(publicKey, previousInValue, outValue, signature,
                            timestamp),
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.NextRound:
                    Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp));
                    Assert(GenerateNextRoundInformation(round, timestamp, blockchainStartTimestamp, out var nextRound),
                        "Failed to generate next round information.");
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.NextTerm:
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = GenerateFirstRoundOfNextTerm(),
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.Invalid:
                    return new DPoSInformation
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
            Assert(input.Timestamp != null, "Data to request consensus information should contain timestamp.");

            var publicKey = input.PublicKey;

            var consensusInformation = GetNewConsensusInformation(input);

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
                case DPoSBehaviour.UpdateValue:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue), round.GenerateToUpdate(publicKey))
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
                case DPoSBehaviour.NextTerm:
                    Assert(TryToGetRoundNumber(out var roundNumber), "Failed to get current round number.");
                    Assert(TryToGetTermNumber(out var termNumber), "Failed to get current term number.");
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction("NextTerm", round),
                            GenerateTransaction("SnapshotForMiners",
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
                            GenerateTransaction("SnapshotForTerm",
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
                            GenerateTransaction("SendDividends",
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
                        }
                    };
                case DPoSBehaviour.Invalid:
                    return new TransactionList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ValidationResult ValidateConsensusBeforeExecution(DPoSInformation input)
        {
            var message = "";
            try
            {
                var publicKey = input.SenderPublicKey;

                // Validate the sender.
                if (TryToGetCurrentRoundInformation(out var currentRound) &&
                    !currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
                {
                    return new ValidationResult {Success = false, Message = "Sender is not a miner."};
                }

                var behaviour = input.Behaviour;

                var successToGetCurrentRound = currentRound != null;

                switch (behaviour)
                {
                    case DPoSBehaviour.InitialConsensus:
                        break;
                    case DPoSBehaviour.UpdateValue:
                        if (!successToGetCurrentRound)
                        {
                            return new ValidationResult
                                {Success = false, Message = "Failed to get current round information."};
                        }

                        if (!RoundIdMatched(input.Round))
                        {
                            return new ValidationResult {Success = false, Message = "Round Id not match."};
                        }

                        if (!NewOutValueFilled(input.Round))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                        }

                        break;
                    case DPoSBehaviour.NextRound:
                        if (!successToGetCurrentRound)
                        {
                            return new ValidationResult
                                {Success = false, Message = "Failed to get current round information."};
                        }

                        // None of in values should be filled.
                        if (!InValueIsNull(input.Round))
                        {
                            return new ValidationResult {Success = false, Message = "Incorrect in values."};
                        }

                        break;
                    case DPoSBehaviour.NextTerm:
                        if (!successToGetCurrentRound)
                        {
                            return new ValidationResult
                                {Success = false, Message = "Failed to get current round information."};
                        }

                        break;
                    case DPoSBehaviour.Invalid:
                        return new ValidationResult {Success = false, Message = "Invalid behaviour."};
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }catch(Exception ex)
            {
                message = ex.ToString();
                Context.LogDebug(()=> ex.StackTrace);
                return new ValidationResult{Success = false, Message = message};
            }

            return new ValidationResult {Success = true};
        }

        public override ValidationResult ValidateConsensusAfterExecution(DPoSInformation input)
        {
            // TODO: To implement.
            return new ValidationResult {Success = true};
        }

        /// <summary>
        /// Get next consensus behaviour of the caller based on current state.
        /// This method can be tested by testing GetConsensusCommand.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="timestamp"></param>
        /// <param name="round"></param>
        /// <param name="minerInRound"></param>
        /// <returns></returns>
        private DPoSBehaviour GetBehaviour(string publicKey, Timestamp timestamp, out Round round,
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

            if (!round.IsTimeSlotPassed(publicKey, timestamp, out minerInRound) && minerInRound.OutValue == null)
            {
                return minerInRound != null ? DPoSBehaviour.UpdateValue : DPoSBehaviour.Invalid;
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
            return round.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp, termNumber)
                ? DPoSBehaviour.NextTerm
                : DPoSBehaviour.NextRound;
        }

        private string GetLogStringForOneRound(string publicKey)
        {
            if (!TryToGetCurrentRoundInformation(out var round))
            {
                return "";
            }

            var logs = $"\n[Round {round.RoundNumber}](Round Id: {round.RoundId})";
            foreach (var minerInRound in round.RealTimeMinersInformation.Values.OrderBy(m => m.Order))
            {
                var minerInformation = "\n";
                minerInformation += $"[{minerInRound.PublicKey.Substring(0, 10)}]";
                minerInformation += minerInRound.IsExtraBlockProducer ? "(Current EBP)" : "";
                minerInformation +=
                    minerInRound.PublicKey == publicKey
                        ? "(This Node)"
                        : "";
                minerInformation += $"\nOrder:\t {minerInRound.Order}";
                minerInformation +=
                    $"\nTime:\t {minerInRound.ExpectedMiningTime.ToDateTime().ToUniversalTime():yyyy-MM-dd HH.mm.ss,fff}";
                minerInformation += $"\nOut:\t {minerInRound.OutValue?.ToHex()}";
                if (round.RoundNumber != 1)
                {
                    minerInformation += $"\nPreIn:\t {minerInRound.PreviousInValue?.ToHex()}";
                }

                minerInformation += $"\nSig:\t {minerInRound.Signature?.ToHex()}";
                minerInformation += $"\nMine:\t {minerInRound.ProducedBlocks}";
                minerInformation += $"\nMiss:\t {minerInRound.MissedTimeSlots}";
                minerInformation += $"\nProms:\t {minerInRound.PromisedTinyBlocks}";
                minerInformation += $"\nNOrder:\t {minerInRound.OrderOfNextRound}";

                logs += minerInformation;
            }

            return logs;
        }
    }
}