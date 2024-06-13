using System;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class AEDPoSContract
{
    public override Int32Value GetMaximumMinersCount(Empty input)
    {
        return new Int32Value
        {
            Value = Math.Min(GetAutoIncreasedMinersCount(), State.MaximumMinersCount.Value)
        };
    }

    private int GetAutoIncreasedMinersCount()
    {
        if (State.BlockchainStartTimestamp.Value == null)
        {
            return AEDPoSContractConstants.SupposedMinersCount;
        }

        return AEDPoSContractConstants.SupposedMinersCount.Add(
            (int) (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
            .Div(State.MinerIncreaseInterval.Value).Mul(2));
    }
}