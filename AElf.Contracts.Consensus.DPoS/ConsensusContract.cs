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
    public partial class ConsensusContract : CSharpSmartContract<DPoSContractState>, IConsensusSmartContract
    {
        // This file contains implementations of IConsensusSmartContract.
        
        [View]
        public byte[] GetConsensusCommand(byte[] consensusTriggerInformation)
        {
            var payload = DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);

            // Some basic checks.
            Assert(payload.PublicKey.Any(), "Trigger information should contain public key.");
            Assert(payload.Timestamp != null, "Trigger information should contain timestamp.");

            var publicKey = payload.PublicKey;
            var timestamp = payload.Timestamp;

            Context.LogDebug(() => GetLogStringForOneRound(publicKey));

            var behaviour = GetBehaviour(publicKey, timestamp, out var round, out var minerInRound);

            TryToGetMiningInterval(out var miningInterval);
            return behaviour.GetConsensusCommand(round, minerInRound, miningInterval, timestamp, payload.IsBootMiner)
                .ToByteArray();
        }

        [View]
        public byte[] GetNewConsensusInformation(byte[] consensusTriggerInformation)
        {
            var payload = DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);

            // Some basic checks.
            Assert(payload.PublicKey.Any(), "Data to request consensus information should contain public key.");
            Assert(payload.Timestamp != null, "Data to request consensus information should contain timestamp.");

            var publicKey = payload.PublicKey;
            var timestamp = payload.Timestamp;

            var behaviour = GetBehaviour(publicKey, timestamp, out var round, out _);

            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    var miningInterval = payload.MiningInterval;
                    var initialMiners = payload.Miners;
                    var firstRound = initialMiners.ToList().ToMiners(1).GenerateFirstRoundOfNewTerm(miningInterval);
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = firstRound,
                        Behaviour = behaviour
                    }.ToByteArray();
                case DPoSBehaviour.UpdateValue:
                    Assert(payload.CurrentInValue != null && payload.CurrentInValue != null,
                        "Current in value should be valid.");

                    var previousInValue = payload.PreviousInValue;

                    var inValue = payload.CurrentInValue;

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
                    }.ToByteArray();
                case DPoSBehaviour.NextRound:
                    Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp));
                    Assert(GenerateNextRoundInformation(round, timestamp, blockchainStartTimestamp, out var nextRound),
                        "Failed to generate next round information.");
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    }.ToByteArray();
                case DPoSBehaviour.NextTerm:
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = GenerateFirstRoundOfNextTerm(),
                        Behaviour = behaviour
                    }.ToByteArray();
                case DPoSBehaviour.Invalid:
                    return new DPoSInformation
                    {
                        SenderPublicKey = publicKey,
                        Behaviour = behaviour
                    }.ToByteArray();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [View]
        public TransactionList GenerateConsensusTransactions(byte[] consensusTriggerInformation)
        {
            var payload = DPoSTriggerInformation.Parser.ParseFrom(consensusTriggerInformation);

            // Some basic checks.
            Assert(payload.PublicKey.Any(), "Data to request consensus information should contain public key.");
            Assert(payload.Timestamp != null, "Data to request consensus information should contain timestamp.");

            var publicKey = payload.PublicKey;

            var consensusInformationBytes = GetNewConsensusInformation(consensusTriggerInformation);

            var consensusInformation = DPoSInformation.Parser.ParseFrom(consensusInformationBytes);

            var round = consensusInformation.Round;

            var behaviour = consensusInformation.Behaviour;

            switch (behaviour)
            {
                case DPoSBehaviour.InitialConsensus:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(InitialConsensus),
                                new List<object> {round})
                        }
                    };
                case DPoSBehaviour.UpdateValue:
                    var minerInRound = round.RealTimeMinersInformation[publicKey];
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(UpdateValue),
                                new List<object>
                                {
                                    new ToUpdate
                                    {
                                        OutValue = minerInRound.OutValue,
                                        Signature = minerInRound.Signature,
                                        PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                                        RoundId = round.RoundId,
                                        PromiseTinyBlocks = minerInRound.PromisedTinyBlocks,
                                        ActualMiningTime = minerInRound.ActualMiningTime
                                    }
                                }),
                        }
                    };
                case DPoSBehaviour.NextRound:
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction(nameof(NextRound),
                                new List<object> {round})
                        }
                    };
                case DPoSBehaviour.NextTerm:
                    Assert(TryToGetRoundNumber(out var roundNumber), "Failed to get current round number.");
                    Assert(TryToGetTermNumber(out var termNumber), "Failed to get current term number.");
                    return new TransactionList
                    {
                        Transactions =
                        {
                            GenerateTransaction("NextTerm", new List<object> {round}),
                            GenerateTransaction("SnapshotForMiners",
                                new List<object> {termNumber, roundNumber}),
                            GenerateTransaction("SnapshotForTerm",
                                new List<object> {termNumber, roundNumber}),
                            GenerateTransaction("SendDividends",
                                new List<object> {termNumber, roundNumber})
                        }
                    };
                case DPoSBehaviour.Invalid:
                    return new TransactionList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [View]
        public ValidationResult ValidateConsensus(byte[] consensusInformation)
        {
            var information = DPoSInformation.Parser.ParseFrom(consensusInformation);

            var publicKey = information.SenderPublicKey;

            // Validate the sender.
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return new ValidationResult {Success = false, Message = "Sender is not a miner."};
            }

            var behaviour = information.Behaviour;

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

                    if (!RoundIdMatched(information.Round))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    if (!NewOutValueFilled(information.Round))
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
                    if (!InValueIsNull(information.Round))
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
                minerInformation += $"\nProms:\t{minerInRound.PromisedTinyBlocks}";

                logs += minerInformation;
            }

            return logs;
        }
    }
}