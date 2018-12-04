using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class ElectionSnapshot
    {
        public Address GetNextCandidate(Miners currentMiners)
        {
            var ranking = TicketsMap.OrderBy(tm => tm.TicketsCount).Select(tm => tm.CandidateAddress).ToList();
            for (var i = GlobalConfig.BlockProducerNumber + 1; i < ranking.Count(); i++)
            {
                if (!currentMiners.Nodes.Contains(ranking[i]))
                {
                    return ranking[i];
                }
            }

            return Address.Zero;
        }
    }
}