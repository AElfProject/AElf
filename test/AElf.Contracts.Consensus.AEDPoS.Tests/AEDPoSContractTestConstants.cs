using System.Diagnostics.CodeAnalysis;
using AElf.CSharp.Core;

namespace AElf.Contracts.Consensus.AEDPoS;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AEDPoSContractTestConstants
{
    internal const int TinySlots = 8;

    internal const int InitialMinersCount = 5;

    internal const int SupposedMinersCount = 17;

    internal const int MiningInterval = 4000;

    /// <summary>
    ///     7 days.
    /// </summary>
    internal const long PeriodSeconds = 120; // 7 * 60 * 60 * 24

    internal static readonly int SmallBlockMiningInterval = MiningInterval.Div(TinySlots)
        .Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
        .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalShares);
}