﻿using System.Linq;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public partial class AEDPoSContract : AEDPoSContractImplContainer.AEDPoSContractImplBase
{
    #region Initial

    /// <summary>
    /// The transaction with this method will generate on every node
    /// and executed with the same result.
    /// Otherwise, the block hash of the genesis block won't be equal.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty InitialAElfConsensusContract(InitialAElfConsensusContractInput input)
    {
        Assert(State.CurrentRoundNumber.Value == 0 && !State.Initialized.Value, "Already initialized.");
        State.Initialized.Value = true;

        State.PeriodSeconds.Value = input.IsTermStayOne
            ? int.MaxValue
            : input.PeriodSeconds;

        State.MinerIncreaseInterval.Value = input.MinerIncreaseInterval;

        Context.LogDebug(() => $"There are {State.PeriodSeconds.Value} seconds per period.");

        if (input.IsTermStayOne || input.IsSideChain)
        {
            State.IsMainChain.Value = false;
            return new Empty();
        }

        State.IsMainChain.Value = true;

        State.MaximumMinersCount.Value = int.MaxValue;

        State.SingleNodeMiningInterval.Value = input.SingleNodeMiningInterval;

        return new Empty();
    }

    #endregion

    #region FirstRound

    /// <summary>
    /// The transaction with this method will generate on every node
    /// and executed with the same result.
    /// Otherwise, the block hash of the genesis block won't be equal.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty FirstRound(Round input)
    {
        /* Basic checks. */
        Assert(State.CurrentRoundNumber.Value == 0, "Already initialized.");

        /* Initial settings. */
        State.CurrentTermNumber.Value = 1;
        State.CurrentRoundNumber.Value = 1;
        State.FirstRoundNumberOfEachTerm[1] = 1;
        State.MiningInterval.Value = input.GetMiningInterval();
        if (input.RealTimeMinersInformation.Count == 1 && State.SingleNodeMiningInterval.Value != 0)
        {
            State.MiningInterval.Value = State.SingleNodeMiningInterval.Value;
        }

        SetMinerList(input.GetMinerList(), 1);

        AddRoundInformation(input);

        Context.LogDebug(() =>
            $"Initial Miners: {input.RealTimeMinersInformation.Keys.Aggregate("\n", (key1, key2) => key1 + "\n" + key2)}");

        return new Empty();
    }

    #endregion

    #region UpdateValue

    public override Empty UpdateValue(UpdateValueInput input)
    {
        ProcessConsensusInformation(input);
        return new Empty();
    }

    #endregion

    #region UpdateTinyBlockInformation

    public override Empty UpdateTinyBlockInformation(TinyBlockInput input)
    {
        ProcessConsensusInformation(input);
        return new Empty();
    }

    #endregion

    #region NextRound

    public override Empty NextRound(Round input)
    {
        SupplyCurrentRoundInformation();
        ProcessConsensusInformation(input);
        return new Empty();
    }

    /// <summary>
    /// To fill up with InValue and Signature if some miners didn't mined during current round.
    /// </summary>
    private void SupplyCurrentRoundInformation()
    {
        var currentRound = GetCurrentRoundInformation(new Empty());
        Context.LogDebug(() => $"Before supply:\n{currentRound.ToString(Context.RecoverPublicKey().ToHex())}");
        var notMinedMiners = currentRound.RealTimeMinersInformation.Values.Where(m => m.OutValue == null).ToList();
        if (!notMinedMiners.Any()) return;
        TryToGetPreviousRoundInformation(out var previousRound);
        foreach (var miner in notMinedMiners)
        {
            Context.LogDebug(() => $"Miner pubkey {miner.Pubkey}");

            Hash previousInValue = null;
            Hash signature = null;

            // Normal situation: previous round information exists and contains this miner.
            if (previousRound != null && previousRound.RealTimeMinersInformation.ContainsKey(miner.Pubkey))
            {
                // Check this miner's:
                // 1. PreviousInValue in current round; (means previous in value recovered by other miners)
                // 2. InValue in previous round; (means this miner hasn't produce blocks for a while)
                previousInValue = currentRound.RealTimeMinersInformation[miner.Pubkey].PreviousInValue;
                if (previousInValue == null)
                {
                    previousInValue = previousRound.RealTimeMinersInformation[miner.Pubkey].InValue;
                }

                // If previousInValue is still null, treat this as abnormal situation.
                if (previousInValue != null)
                {
                    Context.LogDebug(() => $"Previous round: {previousRound.ToString(miner.Pubkey)}");
                    signature = previousRound.CalculateSignature(previousInValue);
                }
            }

            if (previousInValue == null)
            {
                // Handle abnormal situation.

                // The fake in value shall only use once during one term.
                previousInValue = HashHelper.ComputeFrom(miner);
                signature = previousInValue;
            }

            // Fill this two fields at last.
            miner.InValue = previousInValue;
            miner.Signature = signature;

            currentRound.RealTimeMinersInformation[miner.Pubkey] = miner;
        }

        TryToUpdateRoundInformation(currentRound);
        Context.LogDebug(() => $"After supply:\n{currentRound.ToString(Context.RecoverPublicKey().ToHex())}");
    }

    #endregion
}