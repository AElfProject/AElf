using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Election;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public partial class AEDPoSContract
{
    public override Int64Value GetCurrentRoundNumber(Empty input)
    {
        return new() { Value = State.CurrentRoundNumber.Value };
    }

    public override Round GetCurrentRoundInformation(Empty input)
    {
        return TryToGetCurrentRoundInformation(out var currentRound) ? currentRound : new Round();
    }

    public override Round GetRoundInformation(Int64Value input)
    {
        return TryToGetRoundInformation(input.Value, out var round) ? round : new Round();
    }

    public override MinerList GetCurrentMinerList(Empty input)
    {
        return TryToGetCurrentRoundInformation(out var round)
            ? new MinerList
            {
                Pubkeys =
                {
                    round.RealTimeMinersInformation.Keys.Select(k => ByteStringHelper.FromHexString(k))
                }
            }
            : new MinerList();
    }

    public override PubkeyList GetCurrentMinerPubkeyList(Empty input)
    {
        return new()
        {
            Pubkeys = { GetCurrentMinerList(input).Pubkeys.Select(p => p.ToHex()) }
        };
    }

    public override MinerListWithRoundNumber GetCurrentMinerListWithRoundNumber(Empty input)
    {
        return new()
        {
            MinerList = GetCurrentMinerList(new Empty()),
            RoundNumber = State.CurrentRoundNumber.Value
        };
    }

    public override Round GetPreviousRoundInformation(Empty input)
    {
        return TryToGetPreviousRoundInformation(out var previousRound) ? previousRound : new Round();
    }

    public override MinerList GetMinerList(GetMinerListInput input)
    {
        return State.MinerListMap[input.TermNumber] ?? new MinerList();
    }

    public override Int64Value GetMinedBlocksOfPreviousTerm(Empty input)
    {
        if (TryToGetTermNumber(out var termNumber))
        {
            var targetRound = State.FirstRoundNumberOfEachTerm[termNumber].Sub(1);
            if (TryToGetRoundInformation(targetRound, out var round))
                return new Int64Value { Value = round.GetMinedBlocks() };
        }

        return new Int64Value();
    }

    public override MinerList GetPreviousMinerList(Empty input)
    {
        if (TryToGetTermNumber(out var termNumber) && termNumber > 1)
            return State.MinerListMap[termNumber.Sub(1)] ?? new MinerList();

        return new MinerList();
    }

    public override StringValue GetNextMinerPubkey(Empty input)
    {
        if (TryToGetCurrentRoundInformation(out var round))
            return new StringValue
            {
                Value = round.RealTimeMinersInformation.Values
                            .FirstOrDefault(m => m.ExpectedMiningTime > Context.CurrentBlockTime)?.Pubkey ??
                        round.RealTimeMinersInformation.Values.First(m => m.IsExtraBlockProducer).Pubkey
            };

        return new StringValue();
    }

    /// <summary>
    ///     Current implementation can be incorrect if all nodes recovering from
    ///     a strike more than the time of one round, because it's impossible to
    ///     infer a time slot in this situation.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override BoolValue IsCurrentMiner(Address input)
    {
        var pubkey = ConvertAddressToPubkey(input);
        return new BoolValue
        {
            Value = IsCurrentMiner(pubkey)
        };
    }

    /// <summary>
    ///     The address must in miner list.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private string ConvertAddressToPubkey(Address address)
    {
        if (!TryToGetCurrentRoundInformation(out var currentRound)) return null;
        var possibleKeys = currentRound.RealTimeMinersInformation.Keys.ToList();
        if (TryToGetPreviousRoundInformation(out var previousRound))
            possibleKeys.AddRange(previousRound.RealTimeMinersInformation.Keys);

        return possibleKeys.FirstOrDefault(k =>
            Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k)) == address);
    }

    private bool IsCurrentMiner(string pubkey)
    {
        if (pubkey == null) return false;

        if (!TryToGetCurrentRoundInformation(out var currentRound)) return false;

        if (!currentRound.IsMinerListJustChanged)
            if (!currentRound.RealTimeMinersInformation.ContainsKey(pubkey))
                return false;

        Context.LogDebug(() =>
            $"Extra block producer of previous round: {currentRound.ExtraBlockProducerOfPreviousRound}");

        // Check confirmed extra block producer of previous round.
        if (Context.CurrentBlockTime <= currentRound.GetRoundStartTime() &&
            currentRound.ExtraBlockProducerOfPreviousRound == pubkey)
        {
            Context.LogDebug(() => "[CURRENT MINER]PREVIOUS");
            return true;
        }

        var miningInterval = currentRound.GetMiningInterval();
        var minerInRound = currentRound.RealTimeMinersInformation[pubkey];
        var timeSlotStartTime = minerInRound.ExpectedMiningTime;

        // Check normal time slot.
        if (timeSlotStartTime <= Context.CurrentBlockTime && Context.CurrentBlockTime <=
            timeSlotStartTime.AddMilliseconds(miningInterval))
        {
            Context.LogDebug(() => "[CURRENT MINER]NORMAL");
            return true;
        }

        var supposedExtraBlockProducer =
            currentRound.RealTimeMinersInformation.Single(m => m.Value.IsExtraBlockProducer).Key;

        // Check extra block time slot.
        if (Context.CurrentBlockTime >= currentRound.GetExtraBlockMiningTime() &&
            supposedExtraBlockProducer == pubkey)
        {
            Context.LogDebug(() => "[CURRENT MINER]EXTRA");
            return true;
        }

        // Check saving extra block time slot.
        var nextArrangeMiningTime =
            currentRound.ArrangeAbnormalMiningTime(pubkey, Context.CurrentBlockTime, true);
        var actualArrangedMiningTime = nextArrangeMiningTime.AddMilliseconds(-currentRound.TotalMilliseconds());
        if (actualArrangedMiningTime <= Context.CurrentBlockTime &&
            Context.CurrentBlockTime <= actualArrangedMiningTime.AddMilliseconds(miningInterval))
        {
            Context.LogDebug(() => "[CURRENT MINER]SAVING");
            return true;
        }

        // If current round is the first round of current term.
        if (currentRound.RoundNumber == 1)
        {
            Context.LogDebug(() => "First round");

            var latestMinedInfo =
                currentRound.RealTimeMinersInformation.Values.OrderByDescending(i => i.Order)
                    .FirstOrDefault(i => i.ActualMiningTimes.Any() && i.Pubkey != pubkey);
            if (latestMinedInfo != null)
            {
                var minersCount = currentRound.RealTimeMinersInformation.Count;
                var latestMinedSlotLastActualMiningTime = latestMinedInfo.ActualMiningTimes.Last();
                var latestMinedOrder = latestMinedInfo.Order;
                var currentMinerOrder =
                    currentRound.RealTimeMinersInformation.Single(i => i.Key == pubkey).Value.Order;
                var passedSlotsCount =
                    (Context.CurrentBlockTime - latestMinedSlotLastActualMiningTime).Milliseconds()
                    .Div(miningInterval);
                if (passedSlotsCount == currentMinerOrder.Sub(latestMinedOrder).Add(1).Add(minersCount) ||
                    passedSlotsCount == currentMinerOrder.Sub(latestMinedOrder).Add(minersCount))
                {
                    Context.LogDebug(() => "[CURRENT MINER]FIRST ROUND");
                    return true;
                }
            }
        }

        Context.LogDebug(() => "[CURRENT MINER]NOT MINER");

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
            miners.Pubkeys.AddRange(
                currentRound.RealTimeMinersInformation.Keys.Select(k => ByteStringHelper.FromHexString(k)));
            newRound = miners.GenerateFirstRoundOfNewTerm(currentRound.GetMiningInterval(),
                Context.CurrentBlockTime, currentRound);
        }

        newRound.ConfirmedIrreversibleBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
        newRound.ConfirmedIrreversibleBlockRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;

        newRound.BlockchainAge = GetBlockchainAge();

        if (newRound.RealTimeMinersInformation.ContainsKey(senderPubkey))
            newRound.RealTimeMinersInformation[senderPubkey].ProducedBlocks = 1;
        else
            UpdateCandidateInformation(senderPubkey, 1, 0);

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
            Pubkeys = { victoriesPublicKeys.Value }
        };
        return victories.Pubkeys.Any();
    }

    private void GenerateNextRoundInformation(Round currentRound, Timestamp currentBlockTime, out Round nextRound)
    {
        TryToGetPreviousRoundInformation(out var previousRound);
        if (!IsMainChain && IsMainChainMinerListChanged(currentRound))
        {
            nextRound = State.MainChainCurrentMinerList.Value.GenerateFirstRoundOfNewTerm(
                currentRound.GetMiningInterval(), currentBlockTime, currentRound.RoundNumber);
            nextRound.ConfirmedIrreversibleBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
            nextRound.ConfirmedIrreversibleBlockRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;
            return;
        }

        var blockchainStartTimestamp = GetBlockchainStartTimestamp();
        var isMinerListChanged = false;
        if (IsMainChain && previousRound.TermNumber == currentRound.TermNumber) // In same term.
        {
            var minerReplacementInformation = State.ElectionContract.GetMinerReplacementInformation.Call(
                new GetMinerReplacementInformationInput
                {
                    CurrentMinerList = { currentRound.RealTimeMinersInformation.Keys }
                });

            Context.LogDebug(() => $"Got miner replacement information:\n{minerReplacementInformation}");

            if (minerReplacementInformation.AlternativeCandidatePubkeys.Count > 0)
            {
                for (var i = 0; i < minerReplacementInformation.AlternativeCandidatePubkeys.Count; i++)
                {
                    var alternativeCandidatePubkey = minerReplacementInformation.AlternativeCandidatePubkeys[i];
                    var evilMinerPubkey = minerReplacementInformation.EvilMinerPubkeys[i];

                    // Update history information of evil node.
                    UpdateCandidateInformation(evilMinerPubkey,
                        currentRound.RealTimeMinersInformation[evilMinerPubkey].ProducedBlocks,
                        currentRound.RealTimeMinersInformation[evilMinerPubkey].MissedTimeSlots, true);

                    Context.Fire(new MinerReplaced
                    {
                        NewMinerPubkey = alternativeCandidatePubkey
                    });

                    // Transfer evil node's consensus information to the chosen backup.
                    var evilMinerInformation = currentRound.RealTimeMinersInformation[evilMinerPubkey];
                    var minerInRound = new MinerInRound
                    {
                        Pubkey = alternativeCandidatePubkey,
                        ExpectedMiningTime = evilMinerInformation.ExpectedMiningTime,
                        Order = evilMinerInformation.Order,
                        PreviousInValue = Hash.Empty,
                        IsExtraBlockProducer = evilMinerInformation.IsExtraBlockProducer
                    };

                    currentRound.RealTimeMinersInformation.Remove(evilMinerPubkey);
                    currentRound.RealTimeMinersInformation.Add(alternativeCandidatePubkey, minerInRound);
                }

                isMinerListChanged = true;
            }
        }

        currentRound.GenerateNextRoundInformation(currentBlockTime, blockchainStartTimestamp, out nextRound,
            isMinerListChanged);
    }

    private bool IsMainChainMinerListChanged(Round currentRound)
    {
        return State.MainChainCurrentMinerList.Value.Pubkeys.Any() &&
               GetMinerListHash(currentRound.RealTimeMinersInformation.Keys) !=
               GetMinerListHash(State.MainChainCurrentMinerList.Value.Pubkeys.Select(p => p.ToHex()));
    }

    private static Hash GetMinerListHash(IEnumerable<string> minerList)
    {
        return HashHelper.ComputeFrom(
            minerList.OrderBy(p => p).Aggregate("", (current, publicKey) => current + publicKey));
    }

    public override Int64Value GetCurrentTermNumber(Empty input)
    {
        return new Int64Value { Value = State.CurrentTermNumber.Value };
    }

    private void UpdateCandidateInformation(string candidatePublicKey, long recentlyProducedBlocks,
        long recentlyMissedTimeSlots, bool isEvilNode = false)
    {
        if (!State.IsMainChain.Value) return;

        State.ElectionContract.UpdateCandidateInformation.Send(new UpdateCandidateInformationInput
        {
            Pubkey = candidatePublicKey,
            RecentlyProducedBlocks = recentlyProducedBlocks,
            RecentlyMissedTimeSlots = recentlyMissedTimeSlots,
            IsEvilNode = isEvilNode
        });
    }

    private int GetMinersCount(Round input)
    {
        if (State.BlockchainStartTimestamp.Value == null) return AEDPoSContractConstants.SupposedMinersCount;

        if (!TryToGetRoundInformation(1, out _)) return 0;
        return Math.Min(input.RealTimeMinersInformation.Count < AEDPoSContractConstants.SupposedMinersCount
            ? AEDPoSContractConstants.SupposedMinersCount
            : AEDPoSContractConstants.SupposedMinersCount.Add(
                (int)(Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                .Div(State.MinerIncreaseInterval.Value).Mul(2)), State.MaximumMinersCount.Value);
    }

    public override Int64Value GetCurrentTermMiningReward(Empty input)
    {
        if (TryToGetCurrentRoundInformation(out var currentRound))
            return new Int64Value
                { Value = currentRound.GetMinedBlocks().Mul(GetMiningRewardPerBlock()) };

        return new Int64Value { Value = 0 };
    }

    public override Int64Value GetCurrentMiningRewardPerBlock(Empty input)
    {
        return new Int64Value { Value = GetMiningRewardPerBlock() };
    }

    /// <summary>
    ///     Get left seconds to next election takes effects.
    ///     Return 0 for side chain and single node.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Int64Value GetNextElectCountDown(Empty input)
    {
        if (!State.IsMainChain.Value) return new Int64Value();

        var currentTermNumber = State.CurrentTermNumber.Value;
        Timestamp currentTermStartTime;
        if (currentTermNumber == 1)
        {
            currentTermStartTime = State.BlockchainStartTimestamp.Value;
            if (TryToGetRoundInformation(1, out var firstRound) &&
                firstRound.RealTimeMinersInformation.Count == 1)
                return new Int64Value(); // Return 0 for single node.
        }
        else
        {
            var firstRoundNumberOfCurrentTerm = State.FirstRoundNumberOfEachTerm[currentTermNumber];
            if (!TryToGetRoundInformation(firstRoundNumberOfCurrentTerm, out var firstRoundOfCurrentTerm))
                return new Int64Value(); // Unlikely.
            if (firstRoundOfCurrentTerm.RealTimeMinersInformation.Count == 1)
                return new Int64Value(); // Return 0 for single node.
            currentTermStartTime = firstRoundOfCurrentTerm.GetRoundStartTime();
        }

        var currentTermEndTime = currentTermStartTime.AddSeconds(State.PeriodSeconds.Value);
        return new Int64Value { Value = (currentTermEndTime - Context.CurrentBlockTime).Seconds };
    }

    public override Round GetPreviousTermInformation(Int64Value input)
    {
        var lastRoundNumber = State.FirstRoundNumberOfEachTerm[input.Value.Add(1)].Sub(1);
        var round = State.Rounds[lastRoundNumber];
        if (round == null || round.RoundId == 0) return new Round();
        var result = new Round
        {
            TermNumber = input.Value
        };
        foreach (var minerInRound in round.RealTimeMinersInformation)
            result.RealTimeMinersInformation[minerInRound.Key] = new MinerInRound
            {
                Pubkey = minerInRound.Value.Pubkey,
                ProducedBlocks = minerInRound.Value.ProducedBlocks
            };

        return result;
    }

    public override MinerList GetMainChainCurrentMinerList(Empty input)
    {
        return State.MainChainCurrentMinerList.Value;
    }

    public override PubkeyList GetPreviousTermMinerPubkeyList(Empty input)
    {
        var lastRoundNumber = State.FirstRoundNumberOfEachTerm[State.CurrentTermNumber.Value].Sub(1);
        var lastRound = State.Rounds[lastRoundNumber];
        if (lastRound == null || lastRound.RoundId == 0) return new PubkeyList();
        return new PubkeyList
        {
            Pubkeys = { lastRound.RealTimeMinersInformation.Keys }
        };
    }
}