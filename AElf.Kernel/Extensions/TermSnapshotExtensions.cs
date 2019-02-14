using System.Linq;
using AElf.Common;

namespace AElf.Kernel
{
    public static class TermSnapshotExtensions
    {
        public static string GetNextCandidate(this TermSnapshot termSnapshot, Miners currentMiners)
        {
            var ranking = termSnapshot.CandidatesSnapshot.OrderBy(ms => ms.Votes).Select(ms => ms.PublicKey).ToList();
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