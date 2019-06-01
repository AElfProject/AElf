using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Contracts.Election;
using AElf.Cryptography.SecretSharing;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override SInt64Value GetCurrentRoundNumber(Empty input) =>
            new SInt64Value {Value = State.CurrentRoundNumber.Value};

        public override Round GetCurrentRoundInformation(Empty input) =>
            TryToGetCurrentRoundInformation(out var currentRound) ? currentRound : new Round();

        public override Round GetRoundInformation(SInt64Value input) =>
            TryToGetRoundInformation(input.Value, out var round) ? round : new Round();

        public override MinerList GetCurrentMinerList(Empty input) =>
            TryToGetCurrentRoundInformation(out var round)
                ? new MinerList
                {
                    PublicKeys =
                    {
                        round.RealTimeMinersInformation.Keys.Select(k => k.ToByteString())
                    }
                }
                : new MinerList();

        public override MinerListWithRoundNumber GetCurrentMinerListWithRoundNumber(Empty input) =>
            new MinerListWithRoundNumber
            {
                MinerList = GetCurrentMinerList(new Empty()),
                RoundNumber = State.CurrentRoundNumber.Value
            };

        public override Round GetPreviousRoundInformation(Empty input) =>
            TryToGetPreviousRoundInformation(out var previousRound) ? previousRound : new Round();

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
            return (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds;
        }

        private bool TryToGetVictories(out MinerList victories)
        {
            if (!State.IsMainChain.Value)
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

        private void ShareInValueOfCurrentRound(Round currentRound, Round previousRound, Hash inValue, string publicKey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey)) return;

            var minersCount = currentRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var secretShares = SecretSharingHelper.EncodeSecret(inValue.ToHex(), minimumCount, minersCount);
            foreach (var pair in currentRound.RealTimeMinersInformation.OrderBy(m => m.Value.Order)
                .ToDictionary(m => m.Key, m => m.Value.Order))
            {
                // Skip himself.
                if (pair.Key == publicKey) continue;

                var publicKeyOfAnotherMiner = pair.Key;
                var orderOfAnotherMiner = pair.Value;

                // Share in value of current round:

                // Encrypt every secret share with other miner's public key, then fill EncryptedInValues field.
                var plainMessage = Encoding.UTF8.GetBytes(secretShares[orderOfAnotherMiner - 1]);
                var receiverPublicKey = ByteArrayHelpers.FromHexString(publicKeyOfAnotherMiner);
                var encryptedInValue = Context.EncryptMessage(receiverPublicKey, plainMessage);
                currentRound.RealTimeMinersInformation[publicKey].EncryptedInValues
                    .Add(publicKeyOfAnotherMiner, ByteString.CopyFrom(encryptedInValue));
                
                // Decrypt shares published during previous round:

                // First round of every term don't have previous in values.
                if (IsFirstRoundOfCurrentTerm(out _)) continue;

                // Become a miner from this round.
                if (!previousRound.RealTimeMinersInformation.ContainsKey(publicKeyOfAnotherMiner)) continue;

                // No need to decrypt shares of miners who already revealed their previous in values.
                if (currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].PreviousInValue != null) continue;

                var encryptedShares =
                    previousRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].EncryptedInValues;
                if (!encryptedShares.Any()) continue;
                var interestingMessage = encryptedShares[publicKey];
                var senderPublicKey = ByteArrayHelpers.FromHexString(publicKeyOfAnotherMiner);
                // Decrypt another miner's secret share then add a result to this miner's DecryptedInValues field.
                var decryptedInValue = Context.DecryptMessage(senderPublicKey, interestingMessage.ToByteArray());
                currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].DecryptedPreviousInValues
                    .Add(publicKey, ByteString.CopyFrom(decryptedInValue));
            }
        }

        private void RevealSharedInValues(Round currentRound, Round previousRound, string publicKey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey)) return;

            var minersCount = currentRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            foreach (var pair in previousRound.RealTimeMinersInformation.OrderBy(m => m.Value))
            {
                // Skip himself.
                if (pair.Key == publicKey) continue;

                var publicKeyOfAnotherMiner = pair.Key;
                var anotherMinerInPreviousRound = pair.Value;

                if (!anotherMinerInPreviousRound.EncryptedInValues.Any()) continue;
                if (!anotherMinerInPreviousRound.DecryptedPreviousInValues.Any()) continue;

                // Reveal another miner's in value for target round:
                
                var orders = anotherMinerInPreviousRound.DecryptedPreviousInValues.Select((t, i) =>
                        previousRound.RealTimeMinersInformation.Values
                            .First(m => m.PublicKey == anotherMinerInPreviousRound.DecryptedPreviousInValues.Keys.ToList()[i]).Order)
                    .ToList();
                var revealedInValue = Hash.LoadHex(SecretSharingHelper.DecodeSecret(
                    anotherMinerInPreviousRound.DecryptedPreviousInValues.Values.ToList()
                        .Select(s => Encoding.UTF8.GetString(s.ToByteArray())).ToList(),
                    orders, minimumCount));

                Context.LogDebug(() =>
                    $"Revealed in value of {publicKeyOfAnotherMiner} of round {previousRound.RoundNumber}: {revealedInValue}");

                currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].PreviousInValue = revealedInValue;
            }
        }

        private bool GenerateNextRoundInformation(Round currentRound, Timestamp currentBlockTime, out Round nextRound)
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

            var result = currentRound.GenerateNextRoundInformation(currentBlockTime,
                blockchainStartTimestamp, out nextRound);
            return result;
        }

        public override SInt64Value GetCurrentTermNumber(Empty input)
        {
            return new SInt64Value {Value = State.CurrentTermNumber.Value};
        }

        private void UpdateCandidateInformation(string candidatePublicKey, long recentlyProducedBlocks,
            long recentlyMissedTimeSlots, bool isEvilNode = false)
        {
            if (!State.IsMainChain.Value)
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
            if (!State.IsMainChain.Value)
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
                    (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                    .Div(365 * 60 * 60 * 24).Mul(2));
            }

            return 0;
        }
    }
}