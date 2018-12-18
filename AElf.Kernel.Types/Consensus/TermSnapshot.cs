using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TermSnapshot
    {
        public string GetNextCandidate(Miners currentMiners)
        {
            var ranking = CandidatesSnapshot.OrderBy(ms => ms.Votes).Select(ms => ms.PublicKey).ToList();
            for (var i = GlobalConfig.BlockProducerNumber + 1; i < ranking.Count; i++)
            {
                if (!currentMiners.PublicKeys.Contains(ranking[i]))
                {
                    return ranking[i];
                }
            }

            return "";
        }
    }
}