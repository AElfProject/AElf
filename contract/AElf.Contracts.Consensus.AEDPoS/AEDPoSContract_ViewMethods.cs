using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Election;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
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

        public override PubkeyList GetCurrentMinerPubkeyList(Empty input) => new PubkeyList
        {
            Pubkeys = {GetCurrentMinerList(input).Pubkeys.Select(p => p.ToHex())}
        };

        public override MinerListWithRoundNumber GetCurrentMinerListWithRoundNumber(Empty input) =>
            new MinerListWithRoundNumber
            {
                MinerList = GetCurrentMinerList(new Empty()),
                RoundNumber = State.CurrentRoundNumber.Value
            };

        public override Round GetPreviousRoundInformation(Empty input) =>
            TryToGetPreviousRoundInformation(out var previousRound) ? previousRound : new Round();

        public override MinerList GetMinerList(GetMinerListInput input) =>
            State.MinerListMap[input.TermNumber] ?? new MinerList();

        public override SInt64Value GetMinedBlocksOfPreviousTerm(Empty input)
        {
            if (TryToGetTermNumber(out var termNumber))
            {
                var targetRound = State.FirstRoundNumberOfEachTerm[termNumber].Sub(1);
                if (TryToGetRoundInformation(targetRound, out var round))
                {
                    return new SInt64Value {Value = round.GetMinedBlocks()};
                }
            }

            return new SInt64Value();
        }

        public override MinerList GetPreviousMinerList(Empty input)
        {
            if (TryToGetTermNumber(out var termNumber) && termNumber > 1)
            {
                return State.MinerListMap[termNumber.Sub(1)] ?? new MinerList();
            }

            return new MinerList();
        }

        public override StringValue GetNextMinerPubkey(Empty input)
        {
            if (TryToGetCurrentRoundInformation(out var round))
            {
                return new StringValue
                {
                    Value = round.RealTimeMinersInformation.Values
                                .FirstOrDefault(m => m.ExpectedMiningTime > Context.CurrentBlockTime)?.Pubkey ??
                            round.RealTimeMinersInformation.Values.First(m => m.IsExtraBlockProducer).Pubkey
                };
            }

            return new StringValue();
        }

        public override StringValue GetCurrentMinerPubkey(Empty input)
        {
            if (!TryToGetCurrentRoundInformation(out var round)) return new StringValue();
            Context.LogDebug(() => $"Based on round: \n{round.GetSimpleRound()}");
            Context.LogDebug(() => $"Based on block time: {Context.CurrentBlockTime}");
            var currentMinerPubkey = GetCurrentMinerPubkey(round, Context.CurrentBlockTime);
            Context.LogDebug(() => $"Current miner pubkey: {currentMinerPubkey}");
            return currentMinerPubkey != null ? new StringValue {Value = currentMinerPubkey} : new StringValue();
        }

        private string GetCurrentMinerPubkey(Round round, Timestamp currentBlockTime)
        {
            var miningInterval = round.GetMiningInterval();
            string pubkey;
            if (currentBlockTime < round.GetExtraBlockMiningTime())
            {
                pubkey = round.RealTimeMinersInformation.Values.OrderBy(m => m.Order).FirstOrDefault(m =>
                    m.ExpectedMiningTime <= currentBlockTime &&
                    currentBlockTime < m.ExpectedMiningTime.AddMilliseconds(miningInterval))?.Pubkey;
                if (pubkey != null)
                {
                    Context.LogDebug(() => $"Checked normal block time slot: {pubkey}");
                    return pubkey;
                }
            }

            if (!TryToGetPreviousRoundInformation(out var previousRound)) return null;

            Context.LogDebug(() => $"Now based on round: \n{previousRound.GetSimpleRound()}");

            var extraBlockProducer = previousRound.RealTimeMinersInformation.Values.First(m => m.IsExtraBlockProducer)
                .Pubkey;
            var extraBlockMiningTime = previousRound.GetExtraBlockMiningTime();
            if (extraBlockMiningTime <= currentBlockTime &&
                currentBlockTime <= extraBlockMiningTime.AddMilliseconds(miningInterval))
            {
                Context.LogDebug(() => $"Checked extra block time slot: {extraBlockProducer}");
                return extraBlockProducer;
            }

            foreach (var maybeCurrentPubkey in round.RealTimeMinersInformation.Keys.Except(new List<string>
                {extraBlockProducer}))
            {
                var consensusCommand = GetConsensusCommand(AElfConsensusBehaviour.NextRound, round, maybeCurrentPubkey,
                    currentBlockTime.AddMilliseconds(-miningInterval.Mul(round.RealTimeMinersInformation.Count)));
                if (consensusCommand.ArrangedMiningTime <= currentBlockTime && currentBlockTime <=
                    consensusCommand.ArrangedMiningTime.AddMilliseconds(miningInterval))
                {
                    return maybeCurrentPubkey;
                }
            }

            pubkey = previousRound.RealTimeMinersInformation.OrderBy(i => i.Value.Order).Select(i => i.Key)
                .FirstOrDefault(k =>
                    previousRound.IsInCorrectFutureMiningSlot(k, Context.CurrentBlockTime));

            Context.LogDebug(() => $"Checked abnormal extra block time slot: {pubkey}");

            return pubkey;
        }

        public override BoolValue IsCurrentMiner(Address input)
        {
            var result = new BoolValue {Value = IsCurrentMiner(ConvertAddressToPubkey(input))};
            if (result.Value) return result;

            var currentMinerPubkey = GetCurrentMinerPubkey(new Empty());
            if (currentMinerPubkey.Value.Any())
            {
                var isCurrentMiner = new BoolValue
                {
                    Value = input == Address.FromPublicKey(
                                ByteArrayHelper.HexStringToByteArray(currentMinerPubkey.Value))
                };
                Context.LogDebug(() => $"Current miner: {currentMinerPubkey}. {isCurrentMiner}");
                return isCurrentMiner;
            }

            return new BoolValue {Value = false};
        }

        /// <summary>
        /// The address must in miner list.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private string ConvertAddressToPubkey(Address address)
        {
            if (!TryToGetCurrentRoundInformation(out var currentRound)) return null;

            return currentRound.RealTimeMinersInformation.Keys.FirstOrDefault(k =>
                Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k)) == address);
        }

        private bool IsCurrentMiner(string pubkey)
        {
            if (pubkey == null) return false;

            if (!TryToGetCurrentRoundInformation(out var currentRound)) return false;

            if (!currentRound.RealTimeMinersInformation.ContainsKey(pubkey)) return false;

            var currentRoundStartTime = currentRound.GetRoundStartTime();
            if (Context.CurrentBlockTime < currentRoundStartTime)
            {
                return currentRound.ExtraBlockProducerOfPreviousRound == pubkey;
            }

            if (currentRound.IsMinerListJustChanged)
            {
                Context.LogDebug(() => "Term changed and ExtraBlockProducerOfPreviousRound is incorrect.");
            }

            var miningInterval = currentRound.GetMiningInterval();
            var currentRoundExtraBlockMiningTime = currentRound.GetExtraBlockMiningTime();
            var miningInRound = currentRound.RealTimeMinersInformation[pubkey];
            if (currentRoundStartTime <= Context.CurrentBlockTime &&
                Context.CurrentBlockTime < currentRoundExtraBlockMiningTime)
            {
                var supposedMiningTime = miningInRound.ExpectedMiningTime;
                return supposedMiningTime <= Context.CurrentBlockTime &&
                       Context.CurrentBlockTime <= supposedMiningTime.AddMilliseconds(miningInterval);
            }

            if (currentRoundExtraBlockMiningTime <= Context.CurrentBlockTime &&
                Context.CurrentBlockTime < currentRoundExtraBlockMiningTime.AddMilliseconds(miningInterval))
            {
                return currentRound.RealTimeMinersInformation.Single(m => m.Value.IsExtraBlockProducer).Key == pubkey;
            }

            return false;
        }

        private Round GenerateFirstRoundOfNextTerm(string senderPubkey, int miningInterval)
        {
            Round newRound;
            TryToGetCurrentRoundInformation(out var currentRound);

            if (TryToGetVictories(out var victories))
            {
                Context.LogDebug(() => "Got victories successfully.");
                newRound = victories.GenerateFirstRoundOfNewTerm(miningInterval, Context.CurrentBlockTime,
                    currentRound);
            }
            else
            {
                // Miners of new round are same with current round.
                var miners = new MinerList();
                miners.Pubkeys.AddRange(currentRound.RealTimeMinersInformation.Keys.Select(k => k.ToByteString()));
                newRound = miners.GenerateFirstRoundOfNewTerm(currentRound.GetMiningInterval(),
                    Context.CurrentBlockTime, currentRound);
            }

            newRound.ConfirmedIrreversibleBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
            newRound.ConfirmedIrreversibleBlockRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;

            newRound.BlockchainAge = GetBlockchainAge();

            if (newRound.RealTimeMinersInformation.ContainsKey(senderPubkey))
            {
                newRound.RealTimeMinersInformation[senderPubkey].ProducedBlocks = 1;
            }
            else
            {
                UpdateCandidateInformation(senderPubkey, 1, 0);
            }

            newRound.ExtraBlockProducerOfPreviousRound = senderPubkey;

            return newRound;
        }

        private long GetBlockchainAge()
        {
            return State.BlockchainStartTimestamp.Value == null
                ? 0
                : (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds;
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
                "Got victories from Election Contract:\n" +
                $"{string.Join("\n", victoriesPublicKeys.Value.Select(s => s.ToHex().Substring(0, 20)))}");
            victories = new MinerList
            {
                Pubkeys = {victoriesPublicKeys.Value},
            };
            return victories.Pubkeys.Any();
        }

        private bool GenerateNextRoundInformation(Round currentRound, Timestamp currentBlockTime, out Round nextRound)
        {
            TryToGetPreviousRoundInformation(out var previousRound);
            if (!IsMainChain && IsMainChainMinerListChanged(currentRound))
            {
                Context.LogDebug(() => "About to change miners.");
                nextRound = State.MainChainCurrentMinerList.Value.GenerateFirstRoundOfNewTerm(
                    currentRound.GetMiningInterval(), currentBlockTime, currentRound.RoundNumber);
                nextRound.ConfirmedIrreversibleBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
                nextRound.ConfirmedIrreversibleBlockRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;
                Context.LogDebug(() => "Round of new miners generated.");
                return true;
            }

            var blockchainStartTimestamp = GetBlockchainStartTimestamp();
            var isMinerListChanged = false;
            if (previousRound.TermNumber == currentRound.TermNumber) // In same term.
            {
                var evilMinersPublicKey = GetEvilMinersPublicKey(currentRound);
                var evilMinersCount = evilMinersPublicKey.Count;
                if (evilMinersCount != 0)
                {
                    Context.LogDebug(() => $"Evil nodes found: \n{string.Join("\n", evilMinersPublicKey)}");

                    var alternatives = GetNextAvailableMinerPublicKey(currentRound, evilMinersCount);

                    if (alternatives.Count < evilMinersCount)
                    {
                        Context.LogDebug(() => "Failed to find alternative miners.");
                    }
                    else
                    {
                        for (var i = 0; i < evilMinersCount; i++)
                        {
                            // Update history information of evil node.
                            UpdateCandidateInformation(evilMinersPublicKey[i],
                                currentRound.RealTimeMinersInformation[evilMinersPublicKey[i]].ProducedBlocks,
                                currentRound.RealTimeMinersInformation[evilMinersPublicKey[i]].MissedTimeSlots, true);

                            // Transfer evil node's consensus information to the chosen backup.
                            var evilMinerInformation = currentRound.RealTimeMinersInformation[evilMinersPublicKey[i]];
                            var minerInRound = new MinerInRound
                            {
                                Pubkey = alternatives[i],
                                ExpectedMiningTime = evilMinerInformation.ExpectedMiningTime,
                                Order = evilMinerInformation.Order,
                                PreviousInValue = Hash.Empty,
                                IsExtraBlockProducer = evilMinerInformation.IsExtraBlockProducer
                            };

                            currentRound.RealTimeMinersInformation.Add(alternatives[i], minerInRound);
                            currentRound.RealTimeMinersInformation.Remove(evilMinersPublicKey[i]);
                        }

                        isMinerListChanged = true;
                    }
                }
            }

            return currentRound.GenerateNextRoundInformation(currentBlockTime,
                blockchainStartTimestamp, out nextRound, isMinerListChanged);
        }

        private bool IsMainChainMinerListChanged(Round currentRound)
        {
            Context.LogDebug(() => $"MainChainCurrentMinerList: \n{State.MainChainCurrentMinerList.Value}");
            var result = State.MainChainCurrentMinerList.Value.Pubkeys.Any() &&
                         GetMinerListHash(currentRound.RealTimeMinersInformation.Keys) !=
                         GetMinerListHash(State.MainChainCurrentMinerList.Value.Pubkeys.Select(p => p.ToHex()));
            Context.LogDebug(() => $"IsMainChainMinerListChanged: {result}");
            return result;
        }

        private static Hash GetMinerListHash(IEnumerable<string> minerList)
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
                Pubkey = candidatePublicKey,
                RecentlyProducedBlocks = recentlyProducedBlocks,
                RecentlyMissedTimeSlots = recentlyMissedTimeSlots,
                IsEvilNode = isEvilNode
            });
        }

        private List<string> GetEvilMinersPublicKey(Round currentRound)
        {
            var evilMinersPubKey = new List<string>();

            if (State.ElectionContract.Value == null) return evilMinersPubKey;

            // If one miner is not a candidate anymore.
            var candidates = State.ElectionContract.GetCandidates.Call(new Empty()).Value.Select(p => p.ToHex())
                .ToList();
            var initialMiners = State.Rounds[1].RealTimeMinersInformation.Keys;
            if (candidates.Any())
            {
                var keys = currentRound.RealTimeMinersInformation.Keys.Where(pubkey =>
                    !candidates.Contains(pubkey) && !initialMiners.Contains(pubkey));
                evilMinersPubKey.AddRange(keys);
            }

            return evilMinersPubKey;
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

        private List<string> GetNextAvailableMinerPublicKey(Round round, int count = 1)
        {
            var nextCandidate = new List<string>();

            TryToGetRoundInformation(1, out var firstRound);
            // Check out election snapshot.
            if (TryToGetTermNumber(out var termNumber) && termNumber > 1 &&
                TryToGetElectionSnapshot(termNumber - 1, out var snapshot))
            {
                var maybeNextCandidates = snapshot.ElectionResult
                    // Except initial miners.
                    .Where(cs => !firstRound.RealTimeMinersInformation.ContainsKey(cs.Key))
                    // Except current miners.
                    .Where(cs => !round.RealTimeMinersInformation.ContainsKey(cs.Key))
                    .OrderByDescending(s => s.Value)
                    .Where(c => !round.RealTimeMinersInformation.ContainsKey(c.Key)).ToList();
                var take = Math.Min(count, maybeNextCandidates.Count);
                nextCandidate.AddRange(maybeNextCandidates.Select(i => i.Key).Take(take));
                Context.LogDebug(() =>
                    $"Found alternative miner from candidate list: {nextCandidate.Aggregate("\n", (key1, key2) => key1 + "\n" + key2)}");
            }

            // Check out initial miners.
            if (nextCandidate.Count < count)
            {
                nextCandidate.AddRange(firstRound.RealTimeMinersInformation.Keys.Where(k =>
                    !round.RealTimeMinersInformation.ContainsKey(k)).Take(Math.Min(count - nextCandidate.Count,
                    firstRound.RealTimeMinersInformation.Count)));
            }

            return nextCandidate;
        }

        private int GetMinersCount(Round input)
        {
            if (State.BlockchainStartTimestamp.Value == null)
            {
                return AEDPoSContractConstants.SupposedMinersCount;
            }

            if (!TryToGetRoundInformation(1, out _)) return 0;
            return Math.Min(input.RealTimeMinersInformation.Count < AEDPoSContractConstants.SupposedMinersCount
                ? AEDPoSContractConstants.SupposedMinersCount
                : AEDPoSContractConstants.SupposedMinersCount.Add(
                    (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                    .Div(State.MinerIncreaseInterval.Value).Mul(2)), State.MaximumMinersCount.Value);
        }

        public override SInt64Value GetCurrentWelfareReward(Empty input)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return new SInt64Value
                    {Value = currentRound.GetMinedBlocks().Mul(GetMiningRewardPerBlock())};
            }

            return new SInt64Value {Value = 0};
        }

        /// <summary>
        /// Get left seconds to next election takes effects.
        /// Return 0 for side chain and single node.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override SInt64Value GetNextElectCountDown(Empty input)
        {
            if (!State.IsMainChain.Value)
            {
                return new SInt64Value();
            }

            var currentTermNumber = State.CurrentTermNumber.Value;
            Timestamp currentTermStartTime;
            if (currentTermNumber == 1)
            {
                currentTermStartTime = State.BlockchainStartTimestamp.Value;
                if (TryToGetRoundInformation(1, out var firstRound) &&
                    firstRound.RealTimeMinersInformation.Count == 1)
                    return new SInt64Value(); // Return 0 for single node.
            }
            else
            {
                var firstRoundNumberOfCurrentTerm = State.FirstRoundNumberOfEachTerm[currentTermNumber];
                if (!TryToGetRoundInformation(firstRoundNumberOfCurrentTerm, out var firstRoundOfCurrentTerm))
                    return new SInt64Value(); // Unlikely.
                if (firstRoundOfCurrentTerm.RealTimeMinersInformation.Count == 1)
                    return new SInt64Value(); // Return 0 for single node.
                currentTermStartTime = firstRoundOfCurrentTerm.GetRoundStartTime();
            }

            var currentTermEndTime = currentTermStartTime.AddSeconds(State.TimeEachTerm.Value);
            return new SInt64Value {Value = (currentTermEndTime - Context.CurrentBlockTime).Seconds};
        }
    }
}