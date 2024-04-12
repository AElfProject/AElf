using System.Linq;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public partial class AEDPoSContract : AEDPoSContractImplContainer.AEDPoSContractImplBase
{
    #region Initial

    /// <summary>
    ///     The transaction with this method will generate on every node
    ///     and executed with the same result.
    ///     Otherwise, the block hash of the genesis block won't be equal.
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

        if (input.IsSideChain) InitialProfitSchemeForSideChain(input.PeriodSeconds);

        if (input.IsTermStayOne || input.IsSideChain)
        {
            State.IsMainChain.Value = false;
            return new Empty();
        }

        State.IsMainChain.Value = true;

        State.ElectionContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        State.TreasuryContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
        State.TokenContract.Value =
            Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        State.MaximumMinersCount.Value = int.MaxValue;

        if (State.TreasuryContract.Value != null)
            State.TreasuryContract.UpdateMiningReward.Send(new Int64Value
            {
                Value = AEDPoSContractConstants.InitialMiningRewardPerBlock
            });

        return new Empty();
    }

    #endregion

    #region FirstRound

    /// <summary>
    ///     The transaction with this method will generate on every node
    ///     and executed with the same result.
    ///     Otherwise, the block hash of the genesis block won't be equal.
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

    // Keep this for compatibility.
    public override Hash GetRandomHash(Int64Value input)
    {
        Assert(input.Value > 1, "Invalid block height.");
        Assert(Context.CurrentHeight >= input.Value, "Block height not reached.");
        return State.RandomHashes[input.Value] ?? Hash.Empty;
    }

    public override BytesValue GetRandomBytes(BytesValue input)
    {
        var height = new Int64Value();
        height.MergeFrom(input.Value);
        return GetRandomHash(height).ToBytesValue();
    }

    public override Empty RecordCandidateReplacement(RecordCandidateReplacementInput input)
    {
        Assert(Context.Sender == State.ElectionContract.Value,
            "Only Election Contract can record candidate replacement information.");

        if (!TryToGetCurrentRoundInformation(out var currentRound) ||
            !currentRound.RealTimeMinersInformation.ContainsKey(input.OldPubkey)) return new Empty();

        // If this candidate is current miner, need to modify current round information.
        var realTimeMinerInformation = currentRound.RealTimeMinersInformation[input.OldPubkey];
        realTimeMinerInformation.Pubkey = input.NewPubkey;
        currentRound.RealTimeMinersInformation.Remove(input.OldPubkey);
        currentRound.RealTimeMinersInformation.Add(input.NewPubkey, realTimeMinerInformation);
        if (currentRound.ExtraBlockProducerOfPreviousRound == input.OldPubkey)
            currentRound.ExtraBlockProducerOfPreviousRound = input.NewPubkey;
        State.Rounds[State.CurrentRoundNumber.Value] = currentRound;

        // Notify Treasury Contract to update replacement information. (Update from old record.)
        State.TreasuryContract.RecordMinerReplacement.Send(new RecordMinerReplacementInput
        {
            OldPubkey = input.OldPubkey,
            NewPubkey = input.NewPubkey,
            CurrentTermNumber = State.CurrentTermNumber.Value
        });

        return new Empty();
    }

    #region NextRound

    public override Empty NextRound(NextRoundInput input)
    {
        SupplyCurrentRoundInformation();
        ProcessConsensusInformation(input);
        return new Empty();
    }

    /// <summary>
    ///     To fill up with InValue and Signature if some miners didn't mined during current round.
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
                    previousInValue = previousRound.RealTimeMinersInformation[miner.Pubkey].InValue;

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