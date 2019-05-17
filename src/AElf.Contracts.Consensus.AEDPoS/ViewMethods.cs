using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Contracts.Election;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel;
using AElf.Sdk.CSharp;
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

            var behaviour = GetBehaviour(input.Value.ToHex(), Context.CurrentBlockTime, out var currentRound);

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

            if (currentRound == null) return new ConsensusCommand();

            TryToGetPreviousRoundInformation(out var previousRound);

            var command = GetConsensusCommand(behaviour, currentRound, previousRound, input.Value.ToHex(),
                Context.CurrentBlockTime);

            Context.LogDebug(() =>
                currentRound.GetLogs(input.Value.ToHex(),
                    AElfConsensusHint.Parser.ParseFrom(command.Hint).Behaviour));

            return command;
        }

        public override BytesValue GetInformationToUpdateConsensus(
            BytesValue input1)
        {
            var input = new AElfConsensusTriggerInformation();
            input.MergeFrom(input1.Value);
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
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedTinyBlocks = currentRound
                        .RealTimeMinersInformation[publicKey.ToHex()].ProducedTinyBlocks.Add(1);
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks =
                        currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks.Add(1);
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ActualMiningTime =
                        currentBlockTime.ToTimestamp();
                    
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
                        outValue, signature);

                    ShareAndRecoverInValue(updatedRound, previousRound, inValue, publicKey.ToHex());

                    // To publish Out Value.
                    return new BytesValue{Value = new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = updatedRound,
                        Behaviour = behaviour,
                    }.ToByteString()};
                case AElfConsensusBehaviour.TinyBlock:
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedTinyBlocks = currentRound
                        .RealTimeMinersInformation[publicKey.ToHex()].ProducedTinyBlocks.Add(1);
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks =
                        currentRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks.Add(1);
                    currentRound.RealTimeMinersInformation[publicKey.ToHex()].ActualMiningTime =
                        currentBlockTime.ToTimestamp();
                    return new BytesValue{Value = new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = currentRound,
                        Behaviour = behaviour
                    }.ToByteString()};
                case AElfConsensusBehaviour.NextRound:
                    Assert(
                        GenerateNextRoundInformation(currentRound, currentBlockTime, out var nextRound),
                        "Failed to generate next round information.");
                    nextRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks =
                        nextRound.RealTimeMinersInformation[publicKey.ToHex()].ProducedBlocks.Add(1);
                    Context.LogDebug(() => $"Mined blocks: {nextRound.GetMinedBlocks()}");
                    nextRound.ExtraBlockProducerOfPreviousRound = publicKey.ToHex();
                    return new BytesValue{Value = new AElfConsensusHeaderInformation
                    {
                        SenderPublicKey = publicKey,
                        Round = nextRound,
                        Behaviour = behaviour
                    }.ToByteString()};
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
                    return new BytesValue{Value = information.ToByteString()};
                default:
                    return new BytesValue();
            }
        }

        public override TransactionList GenerateConsensusTransactions(BytesValue input1)
        {
            var input = new AElfConsensusTriggerInformation();
            input.MergeFrom(input1.Value);
            // Some basic checks.
            Assert(input.PublicKey.Any(), "Data to request consensus information should contain public key.");

            var publicKey = input.PublicKey;
            var consensusInformation = new AElfConsensusHeaderInformation();
            consensusInformation.MergeFrom(GetInformationToUpdateConsensus(input1).Value);
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
                                    ActualMiningTime = minerInRound.ActualMiningTime,
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

        public override ValidationResult ValidateConsensusBeforeExecution(BytesValue input1)
        {
            var input = new AElfConsensusHeaderInformation();
            input.MergeFrom(input1.Value);
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
                    if (!NewOutValueFilled(input.Round.RealTimeMinersInformation.Values))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }

                    break;
                case AElfConsensusBehaviour.NextRound:
                    // None of in values should be filled.
                    if (input.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
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

        public override SInt64Value GetCurrentRoundNumber(Empty input)
        {
            return new SInt64Value {Value = State.CurrentRoundNumber.Value};
        }

        public override Round GetCurrentRoundInformation(Empty input)
        {
            return TryToGetCurrentRoundInformation(out var currentRound) ? currentRound : new Round();
        }

        public override Round GetRoundInformation(SInt64Value input)
        {
            return TryToGetRoundInformation(input.Value, out var round) ? round : new Round();
        }

        public override MinerList GetCurrentMinerList(Empty input)
        {
            if (TryToGetCurrentRoundInformation(out var round))
            {
                return new MinerList
                {
                    PublicKeys =
                    {
                        round.RealTimeMinersInformation.Keys.Select(k => k.ToByteString())
                    }
                };
            }

            return new MinerList();
        }

        public override MinerListWithRoundNumber GetCurrentMinerListWithRoundNumber(Empty input)
        {
            var minerList = GetCurrentMinerList(new Empty());
            return new MinerListWithRoundNumber
            {
                MinerList = minerList,
                RoundNumber = State.CurrentRoundNumber.Value
            };
        }

        public override Round GetPreviousRoundInformation(Empty input)
        {
            return TryToGetPreviousRoundInformation(out var previousRound) ? previousRound : new Round();
        }

        private bool RoundIdMatched(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return currentRound.RoundId == round.RoundId;
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
                Context.LogDebug(() => "Got victories successfully.");
                round = victories.GenerateFirstRoundOfNewTerm(miningInterval, Context.CurrentBlockTime, roundNumber,
                    termNumber);
            }
            else if (TryToGetCurrentRoundInformation(out round))
            {
                var miners = new MinerList();
                miners.PublicKeys.AddRange(round.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
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
                UpdateCandidateInformation(senderPublicKey, 1, 0);
            }

            return round;
        }

        private long GetBlockchainAge()
        {
            return (Context.CurrentBlockTime.ToTimestamp() - State.BlockchainStartTimestamp.Value).Seconds;
        }

        private bool TryToGetVictories(out MinerList victories)
        {
            if (State.ElectionContractSystemName.Value == null)
            {
                victories = null;
                return false;
            }
            var victoriesPublicKeys = State.ElectionContract.GetVictories.Call(new Empty());
            Context.LogDebug(() =>
                $"Got victories from Election Contract:\n{string.Join("\n", victoriesPublicKeys.Value.Select(s => s.ToHex().Substring(0, 10)))}");
            victories = new MinerList
            {
                PublicKeys = {victoriesPublicKeys.Value},
            };
            return victories.PublicKeys.Any();
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

                // TODO: Know this before.
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

        private bool GenerateNextRoundInformation(Round currentRound, DateTime currentBlockTime, out Round nextRound)
        {
            TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp);
            if (TryToGetPreviousRoundInformation(out var previousRound) &&
                previousRound.TermNumber + 1 != currentRound.TermNumber)
            {
                var evilMinersPublicKey = GetEvilMinersPublicKey(currentRound, previousRound);
                var evilMinersCount = evilMinersPublicKey.Count;
                if (evilMinersCount != 0)
                {
                    Context.LogDebug(() => $"Evil nodes found: \n{string.Join("\n", evilMinersPublicKey)}");
                    foreach (var publicKeyToRemove in evilMinersPublicKey)
                    {
                        var theOneFeelingLucky = GetNextAvailableMinerPublicKey(currentRound);

                        if (theOneFeelingLucky == null)
                        {
                            break;
                        }

                        // Update history information of evil node.
                        UpdateCandidateInformation(publicKeyToRemove,
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

            var result = currentRound.GenerateNextRoundInformation(currentBlockTime.ToTimestamp(), blockchainStartTimestamp, out nextRound);
            return result;
        }

        public override SInt64Value GetCurrentTermNumber(Empty input)
        {
            return new SInt64Value {Value = State.CurrentTermNumber.Value};
        }

        private void UpdateCandidateInformation(string candidatePublicKey, long recentlyProducedBlocks,
            long recentlyMissedTimeSlots, bool isEvilNode = false)
        {
            if (State.ElectionContractSystemName.Value == null)
            {
                return;
            }
            State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
            {
                PublicKey = candidatePublicKey,
                RecentlyProducedBlocks = recentlyProducedBlocks,
                RecentlyMissedTimeSlots = recentlyMissedTimeSlots,
                IsEvilNode = isEvilNode
            });
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
            if (State.ElectionContractSystemName.Value == null)
            {
                snapshot = null;
                return false;
            }
            snapshot = State.ElectionContract.GetTermSnapshot.Call(new GetTermSnapshotInput
            {
                TermNumber = termNumber
            });

            return snapshot.ElectionResult.Any();
        }

        private string GetNextAvailableMinerPublicKey(Round round)
        {
            string nextCandidate = null;

            TryToGetRoundInformation(1, out var firstRound);
            // Check out election snapshot.
            if (TryToGetTermNumber(out var termNumber) && termNumber > 1 &&
                TryToGetElectionSnapshot(termNumber - 1, out var snapshot))
            {
                nextCandidate = snapshot.ElectionResult
                    // Except initial miners.
                    .Where(cs => !firstRound.RealTimeMinersInformation.ContainsKey(cs.Key))
                    // Except current miners.
                    .Where(cs => !round.RealTimeMinersInformation.ContainsKey(cs.Key))
                    .OrderByDescending(s => s.Value)
                    .FirstOrDefault(c => !round.RealTimeMinersInformation.ContainsKey(c.Key)).Key;
            }

            // Check out initial miners.
            return nextCandidate ?? firstRound.RealTimeMinersInformation.Keys.FirstOrDefault(k =>
                       !round.RealTimeMinersInformation.ContainsKey(k));
        }

        private int GetMinersCount()
        {
            if (TryToGetRoundInformation(1, out var firstRound))
            {
                // TODO: Maybe this should according to date, like every July 1st we increase 2 miners.
                var initialMinersCount = firstRound.RealTimeMinersInformation.Count;
                return initialMinersCount.Add(
                    (int) (Context.CurrentBlockTime.ToTimestamp() - State.BlockchainStartTimestamp.Value).Seconds
                        .Div(365 * 60 * 60 * 24).Mul(2));
            }

            return 0;
        }
    }
}