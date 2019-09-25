using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private class LastIrreversibleBlockHeightCalculator
        {
            private readonly Round _currentRound;
            private readonly Round _previousRound;

            public LastIrreversibleBlockHeightCalculator(Round currentRound, Round previousRound)
            {
                _currentRound = currentRound;
                _previousRound = previousRound;
            }

            public void Deconstruct(out long libHeight)
            {
                if (_currentRound.IsEmpty || _previousRound.IsEmpty)
                {
                    libHeight = 0;
                }

                var minedMiners = _currentRound.GetMinedMiners().Select(m => m.Pubkey).ToList();
                var impliedIrreversibleHeights = _previousRound.GetSortedImpliedIrreversibleBlockHeights(minedMiners);
                if (impliedIrreversibleHeights.Count < _currentRound.MinersCountOfConsent)
                {
                    libHeight = 0;
                    return;
                }

                libHeight = impliedIrreversibleHeights[impliedIrreversibleHeights.Count.Sub(1).Div(3)];
            }
        }
    }
}