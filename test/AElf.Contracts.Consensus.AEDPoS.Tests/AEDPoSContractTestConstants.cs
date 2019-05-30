using System.Diagnostics.CodeAnalysis;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class AEDPoSContractTestConstants
    {
        internal const int TinySlots = 8;

        internal const int InitialMinersCount = 5;
        
        internal const int CandidatesCount = 10;
        
        internal const int VotersCount = 10;

        internal const int MiningInterval = 4000;

        internal const string Symbol = "ELF";

        internal const long TotalSupply = 1000_000_000;

        internal static readonly int SmallBlockMiningInterval = MiningInterval.Div(TinySlots)
            .Mul(AEDPoSContractConstants.LimitBlockExecutionTimeWeight)
            .Div(AEDPoSContractConstants.LimitBlockExecutionTimeTotalWeight);

        /// <summary>
        /// 7 days.
        /// </summary>
        internal const long TimeEachTerm = 604800;// 7 * 60 * 60 * 24
    }
}