using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Consensus.AElfConsensus;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public partial class AElfConsensusContract
    {
        public override ConsensusCommand GetConsensusCommand(CommandInput input)
        {
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var behaviour = GetBehaviour(input.PublicKey.ToHex(), Context.CurrentBlockTime, out var currentRound);

            if (behaviour == AElfConsensusBehaviour.Nothing)
            {
                return new ConsensusCommand
                {
                    ExpectedMiningTime = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Hint = ByteString.CopyFrom(new AElfConsensusHint {Behaviour = behaviour}.ToByteArray()),
                    LimitMillisecondsOfMiningBlock = int.MaxValue, NextBlockMiningLeftMilliseconds = int.MaxValue
                };
            }

            Assert(currentRound != null && currentRound.RoundId != 0, "Consensus not initialized.");

            var command = GetConsensusCommand(behaviour, currentRound, input.PublicKey.ToHex(),
                Context.CurrentBlockTime);

            Context.LogDebug(() =>
                currentRound.GetLogs(input.PublicKey.ToHex(),
                    AElfConsensusHint.Parser.ParseFrom(command.Hint).Behaviour));

            return command;
        }

        public override AElfConsensusHeaderInformation GetInformationToUpdateConsensus(
            AElfConsensusTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var publicKey = input.PublicKey;
            var currentBlockTime = Context.CurrentBlockTime;
            var behaviour = input.Behaviour;

            Assert(TryToGetCurrentRoundInformation(out var currentRound),
                "Failed to get current round information.");

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
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
                    return new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour,
                    };
                case AElfConsensusBehaviour.NextRound:
                    Assert(
                        GenerateNextRoundInformation(currentRound, currentBlockTime, out var nextRound),
                        "Failed to generate next round information.");
                    nextRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks += 1;
                    Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
                    nextRound.ExtraBlockProducerOfPreviousRound = publicKey.ToHex();
                    return new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    };
                case AElfConsensusBehaviour.NextTerm:
                    Assert(TryToGetMiningInterval(out var miningInterval), "Failed to get mining interval.");
                    var firstRoundOfNextTerm = GenerateFirstRoundOfNextTerm(publicKey.ToHex(), miningInterval);
                    Assert(firstRoundOfNextTerm.RoundId != 0, "Failed to generate new round information.");
                    var information = new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = firstRoundOfNextTerm,
                        Behaviour = behaviour
                    };
                    return information;
                default:
                    return new AElfConsensusHeaderInformation();
            }
        }

        public override TransactionList GenerateConsensusTransactions(AElfConsensusTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");

            var publicKey = input.PublicKey;
            var consensusInformation = GetInformationToUpdateConsensus(input);
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

        public override ValidationResult ValidateConsensusBeforeExecution(AElfConsensusHeaderInformation input)
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

            if (input.Round.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() !=
                input.Round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
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
                case AElfConsensusBehaviour.NextRound:
                    // None of in values should be filled.
                    // TODO: Modified.
                    if (input.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect in values."};
                    }

                    break;
                case AElfConsensusBehaviour.NextTerm:
                    break;
                case AElfConsensusBehaviour.Nothing:
                    return new ValidationResult {Success = false, Message = "Invalid behaviour"};
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ValidationResult {Success = true};
        }

        private bool RoundIdMatched(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRoundInStateDatabase))
            {
                return currentRoundInStateDatabase.RoundId == round.RoundId;
            }

            return false;
        }

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="minersInformation"></param>
        /// <returns></returns>
        private bool NewOutValueFilled(IEnumerable<MinerInRound> minersInformation)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return currentRound.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
                       minersInformation.Count(info => info.OutValue != null);
            }

            return false;
        }

        public override ValidationResult ValidateConsensusAfterExecution(AElfConsensusHeaderInformation input)
        {
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

        private bool TryToGetMiningInterval(out int miningInterval)
        {
            miningInterval = State.MiningInterval.Value;
            return true;
        }

        private Round GenerateFirstRoundOfNextTerm(string senderPublicKey, int miningInterval)
        {
            Round round;
            if (TryToGetTermNumber(out var termNumber) &&
                TryToGetRoundNumber(out var roundNumber) &&
                TryToGetVictories(out var victories))
            {
                round = victories.GenerateFirstRoundOfNewTerm(miningInterval, Context.CurrentBlockTime, roundNumber,
                    termNumber);
            }
            else if (TryToGetCurrentRoundInformation(out round))
            {
                var miners = new Miners();
                miners.PublicKeys.AddRange(round.RealTimeMinersInformation.Keys.Select(k =>
                    ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k))));
                round = miners.GenerateFirstRoundOfNewTerm(round.GetMiningInterval(), Context.CurrentBlockTime,
                    round.RoundNumber, termNumber);
            }

            round.BlockchainAge = GetBlockchainAge();

            if (round.RealTimeMinersInformation.ContainsKey(senderPublicKey))
            {
                round.RealTimeMinersInformation[senderPublicKey].ProducedBlocks = 1;
            }
            else
            {
                Assert(TryToGetCandidateHistory(senderPublicKey, out var history),
                    "Failed to get sender's history information.");
                history.ProducedBlocks += 1;
                AddOrUpdateMinerHistoryInformation(history);
            }

            return round;
        }

        private long GetBlockchainAge()
        {
            return (long) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value.ToDateTime())
                .TotalHours;
        }

        private bool TryToGetCandidateHistory(string publicKey, out CandidateHistory history)
        {
            // TODO: From Election Contract
            throw new NotImplementedException();
        }

        private void AddOrUpdateMinerHistoryInformation(CandidateHistory history)
        {
            // TODO: To Election Contract
            throw new NotImplementedException();
        }

        private bool TryToGetVictories(out Miners victories)
        {
            // TODO: From Election Contract
            throw new NotImplementedException();
        }

        private void ShareAndRecoverInValue(Round round, Round previousRound, Hash inValue, string publicKey)
        {
            var minersCount = round.RealTimeMinersInformation.Count;
            var minimumCount = (int) (minersCount * 2d / 3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var secretShares = SecretSharingHelper.EncodeSecret(inValue.ToHex(), minimumCount, minersCount);
            foreach (var pair in round.RealTimeMinersInformation.OrderBy(m => m.Value.Order))
            {
                var currentPublicKey = pair.Key;

                if (!round.RealTimeMinersInformation.ContainsKey(publicKey))
                {
                    return;
                }

                if (currentPublicKey == publicKey)
                {
                    continue;
                }

                // Encrypt every secret share with other miner's public key, then fill own EncryptedInValues field.
                var plainMessage = Encoding.UTF8.GetBytes(secretShares[pair.Value.Order - 1]);
                var receiverPublicKey = ByteArrayHelpers.FromHexString(currentPublicKey);
                var encryptedInValue = Context.EncryptMessage(receiverPublicKey, plainMessage);
                round.RealTimeMinersInformation[publicKey].EncryptedInValues
                    .Add(currentPublicKey, ByteString.CopyFrom(encryptedInValue));

                if (previousRound.RoundId == 0 || round.TermNumber != previousRound.TermNumber)
                {
                    continue;
                }

                if (!previousRound.RealTimeMinersInformation.ContainsKey(currentPublicKey))
                {
                    continue;
                }

                var encryptedInValues = previousRound.RealTimeMinersInformation[currentPublicKey].EncryptedInValues;
                if (encryptedInValues.Any())
                {
                    var interestingMessage = encryptedInValues[publicKey];
                    var senderPublicKey = ByteArrayHelpers.FromHexString(currentPublicKey);
                    // Decrypt every miner's secret share then add a result to other miner's DecryptedInValues field.
                    var decryptedInValue = Context.DecryptMessage(senderPublicKey, interestingMessage.ToByteArray());
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
                            .First(m => m.PublicKey == pair.Value.DecryptedPreviousInValues.Keys.ToList()[i]).Order)
                    .ToList();

                var previousInValue = Hash.LoadHex(SecretSharingHelper.DecodeSecret(
                    pair.Value.DecryptedPreviousInValues.Values.ToList()
                        .Select(s => Encoding.UTF8.GetString(s.ToByteArray())).ToList(),
                    orders, minimumCount));
                if (round.RealTimeMinersInformation[pair.Key].PreviousInValue != null &&
                    round.RealTimeMinersInformation[pair.Key].PreviousInValue != previousInValue)
                {
                    Context.LogDebug(() => $"Different previous in value: {pair.Key}");
                }

                round.RealTimeMinersInformation[pair.Key].PreviousInValue = previousInValue;
            }
        }

        private bool GenerateNextRoundInformation(Round currentRound, DateTime blockTime, out Round nextRound)
        {
            TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp);
            if (TryToGetPreviousRoundInformation(out var previousRound))
            {
                var evilMinersPublicKey = GetEvilMinersPublicKey(currentRound, previousRound);
                var evilMinersCount = evilMinersPublicKey.Count;
                if (evilMinersCount != 0)
                {
                    foreach (var publicKeyToRemove in evilMinersPublicKey)
                    {
                        var theOneFeelingLucky = GetNextAvailableMinerPublicKey(currentRound);

                        if (theOneFeelingLucky == null)
                        {
                            break;
                        }

                        // Update history information of evil node.
                        UpdateCandidateHistory(publicKeyToRemove,
                            currentRound.RealTimeMinersInformation[publicKeyToRemove].ProducedBlocks,
                            currentRound.RealTimeMinersInformation[publicKeyToRemove].MissedTimeSlots, true);

                        // Transfer evil node's consensus information to the chosen backup.
                        var minerInRound = currentRound.RealTimeMinersInformation[publicKeyToRemove];
                        minerInRound.PublicKey = theOneFeelingLucky;
                        minerInRound.ProducedBlocks = 0;
                        minerInRound.MissedTimeSlots = 0;
                        currentRound.RealTimeMinersInformation[theOneFeelingLucky] = minerInRound;

                        currentRound.RealTimeMinersInformation.Remove(publicKeyToRemove);
                    }
                }
            }

            var result = currentRound.GenerateNextRoundInformation(blockTime, blockchainStartTimestamp, out nextRound);
            return result;
        }

        private void UpdateCandidateHistory(string candidatePublicKey, long recentProducedBlocks,
            long recentMissedTimeSlots, bool isEvilNode = false)
        {
//            var history = State.HistoryMap[publicKeyToRemove.ToStringValue()];
//            history.ProducedBlocks +=
//                currentRound.RealTimeMinersInformation[publicKeyToRemove].ProducedBlocks;
//            history.MissedTimeSlots += 
//                currentRound.RealTimeMinersInformation[publicKeyToRemove].MissedTimeSlots;
//            history.IsEvilNode = true;
//            State.HistoryMap[publicKeyToRemove.ToStringValue()] = history;
            throw new NotImplementedException();
        }

        private List<string> GetEvilMinersPublicKey(Round currentRound, Round previousRound)
        {
            return (from minerInCurrentRound in currentRound.RealTimeMinersInformation.Values
                where previousRound.RealTimeMinersInformation.ContainsKey(minerInCurrentRound.PublicKey) &&
                      minerInCurrentRound.PreviousInValue != null
                let previousOutValue = previousRound.RealTimeMinersInformation[minerInCurrentRound.PublicKey].OutValue
                where previousOutValue != null &&
                      Hash.FromMessage(minerInCurrentRound.PreviousInValue) != previousOutValue
                select minerInCurrentRound.PublicKey).ToList();
        }

        private bool TryToGetElectionSnapshot(long termNumber, out TermSnapshot snapshot)
        {
            // TODO: From Election Contract
            throw new NotImplementedException();
        }

        private string GetNextAvailableMinerPublicKey(Round round)
        {
            string nextCandidate = null;

            TryToGetRoundInformation(1, out var firstRound);
            // Check out election snapshot.
            if (TryToGetTermNumber(out var termNumber) && termNumber > 1 &&
                TryToGetElectionSnapshot(termNumber - 1, out var snapshot))
            {
                nextCandidate = snapshot.CandidatesSnapshot
                    // Except initial miners.
                    .Where(cs => !firstRound.RealTimeMinersInformation.ContainsKey(cs.PublicKey))
                    // Except current miners.
                    .Where(cs => !round.RealTimeMinersInformation.ContainsKey(cs.PublicKey))
                    .OrderByDescending(s => s.Votes)
                    .FirstOrDefault(c => !round.RealTimeMinersInformation.ContainsKey(c.PublicKey))?.PublicKey;
            }

            // Check out initial miners.
            return nextCandidate ?? firstRound.RealTimeMinersInformation.Keys.FirstOrDefault(k =>
                       !round.RealTimeMinersInformation.ContainsKey(k));
        }
    }
}