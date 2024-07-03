using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.PoA;

public partial class PoAContract
{
    public override Empty Initialize(InitializeInput input)
    {
        Context.LogDebug(() => "PoA Contract initialized.");
        State.MiningInterval.Value = input.MiningInterval == 0 ? 4000 : input.MiningInterval;
        return new Empty();
    }

    /// <summary>
    /// Not used for now.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty Mine(MineInput input)
    {
        State.InitialMiner.Value = Context.Sender;
        State.LastMiningTime.Value = Context.CurrentBlockTime;
        return new Empty();
    }

    public override Address GetMiner(Empty input)
    {
        return State.InitialMiner.Value;
    }
}