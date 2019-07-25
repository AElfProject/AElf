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
                    Pubkeys =
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
                miners.Pubkeys.AddRange(round.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
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
                Pubkeys = {victoriesPublicKeys.Value},
            };
            return victories.Pubkeys.Any();
        }

        private bool GenerateNextRoundInformation(Round currentRound, Timestamp currentBlockTime, out Round nextRound)
        {
            if (!State.IsMainChain.Value && IsMainChainMinerListChanged(currentRound))
            {
                Context.LogDebug(() => "About to change miners.");
                nextRound = State.MainChainCurrentMinerList.Value.GenerateFirstRoundOfNewTerm(
                    currentRound.GetMiningInterval(), currentBlockTime, currentRound.RoundNumber);
                Context.LogDebug(() => "Round of new miners generated.");
                return true;
            }

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
                        minerInRound.Pubkey = theOneFeelingLucky;
                        minerInRound.ProducedBlocks = 0;
                        minerInRound.MissedTimeSlots = 0;
                        currentRound.RealTimeMinersInformation[theOneFeelingLucky] = minerInRound;

                        currentRound.RealTimeMinersInformation.Remove(publicKeyToRemove);
                    }
                }
            }

            return currentRound.GenerateNextRoundInformation(currentBlockTime,
                blockchainStartTimestamp, out nextRound);
        }

        private bool IsMainChainMinerListChanged(Round currentRound)
        {
            Context.LogDebug(() => "Entered IsMainChainMinerListChanged.");
            return State.MainChainCurrentMinerList.Value.Pubkeys.Any() &&
                   GetMinerListHash(currentRound.RealTimeMinersInformation.Keys) !=
                   GetMinerListHash(State.MainChainCurrentMinerList.Value.Pubkeys.Select(p => p.ToHex()));
        }

        private Hash GetMinerListHash(IEnumerable<string> minerList)
        {
            return Hash.FromString(
                minerList.OrderBy(p => p).Aggregate("", (current, publicKey) => current + publicKey));
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
                where previousRound.RealTimeMinersInformation.ContainsKey(minerInCurrentRound.Pubkey) &&
                      minerInCurrentRound.PreviousInValue != null
                let previousOutValue = previousRound.RealTimeMinersInformation[minerInCurrentRound.Pubkey].OutValue
                where previousOutValue != null &&
                      Hash.FromMessage(minerInCurrentRound.PreviousInValue) != previousOutValue
                select minerInCurrentRound.Pubkey).ToList();
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


        private int GetMinersCount(Round input)
        {
            if (!TryToGetRoundInformation(1, out _)) return 0;
            // TODO: the configuration about the minercountinterval should become a const when online;
            return input.RealTimeMinersInformation.Count < AEDPoSContractConstants.MinMinersCount
                ? AEDPoSContractConstants.MinMinersCount
                : AEDPoSContractConstants.MinMinersCount.Add(
                    (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                    .Div(State.MinerIncreaseInterval.Value).Mul(2));
        }
    }
}