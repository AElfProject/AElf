using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public List<long> GetSortedImpliedIrreversibleBlockHeights(List<string> specificPublicKeys)
        {
            var heights = RealTimeMinersInformation.Values.Where(i => specificPublicKeys.Contains(i.Pubkey))
                .Where(i => i.ImpliedIrreversibleBlockHeight > 0)
                .Select(i => i.ImpliedIrreversibleBlockHeight).ToList();
            heights.Sort();
            return heights;
        }

        public int GetMinimumMinersCount()
        {
            return RealTimeMinersInformation.Count.Mul(2).Div(3).Add(1);
        }
    }
}