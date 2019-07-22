using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private long CalculateLastIrreversibleBlockHeight()
        {
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                TryToGetPreviousRoundInformation(out var previousRound))
            {
                var minedMiners = currentRound.GetMinedMiners().Select(m => m.Pubkey).ToList();
                var impliedIrreversibleHeights = previousRound.GetSortedImpliedIrreversibleBlockHeights(minedMiners);
                var minimumMinersCount = currentRound.GetMinimumMinersCount();
                Context.LogDebug(() => $"impliedIrreversibleHeights count: {impliedIrreversibleHeights.Count}");
                if (impliedIrreversibleHeights.Count < minimumMinersCount) return 0;
                var libHeight = impliedIrreversibleHeights[impliedIrreversibleHeights.Count.Sub(1).Div(3)];
                Context.LogDebug(() => $"lib height confirmed: {libHeight}");
                return libHeight;
            }

            return 0;
        }
    }
}