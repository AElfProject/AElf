using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Consensus.DPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract
    {
        public override DPoSHeaderInformation GetInformationToUpdateConsensus(DPoSTriggerInformation input)
        {
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var publicKey = input.PublicKey;
            var currentBlockTime = Context.CurrentBlockTime;
            var behaviour = input.Behaviour;

            Assert(TryToGetCurrentRoundInformation(out var currentRound),
                ContractErrorCode.GetErrorMessage(ContractErrorCode.AttemptFailed,
                    "Failed to get current round information."));

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
                        LogVerbose($"Previous random hash: {input.PreviousRandomHash.ToHex()}");
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
                    Assert(
                        GenerateNextRoundInformation(currentRound, currentBlockTime, out var nextRound),
                        "Failed to generate next round information.");
                    nextRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks += 1;
                    Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
                    nextRound.ExtraBlockProducerOfPreviousRound = publicKey.ToHex();
                    return new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    };
                case DPoSBehaviour.NextTerm:
                    var firstRoundOfNextTerm = GenerateFirstRoundOfNextTerm(publicKey.ToHex());
                    Assert(firstRoundOfNextTerm.RoundId != 0, "Failed to generate new round information.");
                    var information = new DPoSHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = firstRoundOfNextTerm,
                        Behaviour = behaviour
                    };
                    return information;
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
                        var history = State.HistoryMap[publicKeyToRemove.ToStringValue()];
                        history.ProducedBlocks +=
                            currentRound.RealTimeMinersInformation[publicKeyToRemove].ProducedBlocks;
                        history.MissedTimeSlots += 
                            currentRound.RealTimeMinersInformation[publicKeyToRemove].MissedTimeSlots;
                        history.IsEvilNode = true;
                        State.HistoryMap[publicKeyToRemove.ToStringValue()] = history;

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

        private List<string> GetEvilMinersPublicKey(Round currentRound, Round previousRound)
        {
            return (from minerInCurrentRound in currentRound.RealTimeMinersInformation.Values
                where previousRound.RealTimeMinersInformation.ContainsKey(minerInCurrentRound.PublicKey) && minerInCurrentRound.PreviousInValue != null
                let previousOutValue = previousRound.RealTimeMinersInformation[minerInCurrentRound.PublicKey].OutValue
                where previousOutValue != null && Hash.FromMessage(minerInCurrentRound.PreviousInValue) != previousOutValue
                select minerInCurrentRound.PublicKey).ToList();
        }

        private string GetNextAvailableMinerPublicKey(Round round)
        {
            string nextCandidate = null;

            TryToGetRoundInformation(1, out var firstRound);
            // Check out election snapshot.
            if (TryToGetTermNumber(out var termNumber) && termNumber > 1 &&
                TryToGetSnapshot(termNumber - 1, out var snapshot))
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
    }
}