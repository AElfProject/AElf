using System;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable UnusedMember.Global
    public partial class ConsensusContract : ConsensusContractContainer.ConsensusContractBase
    {
        public override ConsensusCommand GetConsensusCommand(PublicKey input)
        {
            Assert(input.Hex != string.Empty, "Invalid public key.");
            var behaviour = GetBehaviour(input.Hex, Context.CurrentBlockTime, out var currentRound);
            Context.LogDebug(() => currentRound.GetLogs(input.Hex));
            return behaviour.GetConsensusCommand(currentRound, input.Hex, Context.CurrentBlockTime);
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
                    var signature = Hash.FromTwoHashes(outValue, input.RandomHash); // Just initial signature value.
                    var previousInValue = Hash.Empty; // Just initial previous in value.

                    if (TryToGetPreviousRoundInformation(out var previousRound) && !IsJustChangedTerm(out _))
                    {
                        signature = previousRound.CalculateSignature(inValue);
                        if (input.PreviousRandomHash != Hash.Empty)
                        {
                            // If PreviousRandomHash is Hash.Empty, it means the sender unable or unwilling to publish his previous in value.
                            previousInValue = previousRound.CalculateInValue(input.PreviousRandomHash);
                        }
                    }

                    var updatedRound = currentRound.ApplyNormalConsensusData(publicKey.ToHex(), previousInValue,
                        outValue, signature, currentBlockTime);
                    ShareAndRecoverInValue(updatedRound, previousRound, inValue, publicKey.ToHex());
                    // To publish Out Value.
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour,
                    };
                case DPoSBehaviour.NextRound:
                    Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                        "Failed to get blockchain start timestamp.");
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
                    return new DPoSHeaderInformation();
            }
        }

        private void ShareAndRecoverInValue(Round round, Round previousRound, Hash inValue, string publicKey)
        {
            var minersCount = round.RealTimeMinersInformation.Count;
            var minimumCount = (int) (minersCount * 2d / 3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var secretShares = SecretSharingHelper.EncodeSecret(inValue.ToHex(), minimumCount, minersCount);
            foreach (var pair in round.RealTimeMinersInformation.OrderBy(m => m.Value.Order))
            {
                var key = pair.Key;
                if (key == publicKey)
                {
                    continue;
                }

                // Encrypt every secret share with other miner's public key, then fill own EncryptedInValues field.
                var encryptedInValue = Context.EncryptMessage(ByteArrayHelpers.FromHexString(key),
                    ByteString.FromBase64(secretShares[pair.Value.Order - 1])
                        .ToByteArray());
                round.RealTimeMinersInformation[publicKey].EncryptedInValues
                    .Add(key, ByteString.CopyFrom(encryptedInValue));
                
                if (previousRound.RoundId == 0 || round.TermNumber != previousRound.TermNumber)
                {
                    continue;
                }

                var previousRoundMinersInformation = previousRound.RealTimeMinersInformation;
                if (previousRoundMinersInformation[key].EncryptedInValues.Any())
                {
                    // Decrypt every miner's secret share then add a result to other miner's DecryptedInValues field.
                    var decryptedInValue = Context.DecryptMessage(ByteArrayHelpers.FromHexString(key),
                        previousRoundMinersInformation[key].EncryptedInValues[publicKey].ToByteArray());
                    round.RealTimeMinersInformation[pair.Key].DecryptedPreviousInValues
                        .Add(publicKey, ByteString.CopyFrom(decryptedInValue));
                }

                if (pair.Value.DecryptedPreviousInValues.Count < minimumCount)
                {
                    continue;
                }

                Context.LogDebug(() => "Now it's enough to recover previous in values.");
                
                // Try to recover others' previous in value.
                var orders = pair.Value.DecryptedPreviousInValues.Select((t, i) =>
                    previousRound.RealTimeMinersInformation.Values
                        .First(m => m.PublicKey == pair.Value.DecryptedPreviousInValues.Keys.ToList()[i]).Order).ToList();

                var previousInValue = Hash.LoadHex(SecretSharingHelper.DecodeSecret(
                    pair.Value.DecryptedPreviousInValues.Values.ToList().Select(s => s.ToByteArray().ToHex()).ToList(),
                    orders, minimumCount));
                round.RealTimeMinersInformation[pair.Key].PreviousInValue = previousInValue;
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
                case DPoSBehaviour.NextTerm:
                    Assert(TryToGetRoundNumber(out var roundNumber), "Failed to get current round number.");
                    Assert(TryToGetTermNumber(out var termNumber), "Failed to get current term number.");
                    var nextTermTx = GenerateTransaction("NextTerm", round);
                    if (State.DividendContract.Value == null)
                    {
                        // If dividend contract not deployed.
                        return new TransactionList {Transactions = {nextTermTx}};
                    }
                    return new TransactionList
                    {
                        Transactions =
                        {
                            nextTermTx,
                            GenerateTransaction(nameof(SnapshotForMiners),
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
                            GenerateTransaction(nameof(SnapshotForTerm),
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
                            GenerateTransaction(nameof(SendDividends),
                                new TermInfo {TermNumber = termNumber, RoundNumber = roundNumber}),
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

            if (input.Round.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct().Count() !=
                input.Round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                return new ValidationResult
                    {Success = false, Message = "Invalid FinalOrderOfNextRound."};
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
                case DPoSBehaviour.Watch:
                    return new ValidationResult {Success = false, Message = "Sender is not a miner."};
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
                    return new ValidationResult
                    {
                        Success = false, Message = "Current round information is different with consensus extra data."
                    };
                }
            }

            // TODO: Still need to check: ProducedBlocks,

            return new ValidationResult {Success = true};
        }

        
    }
}