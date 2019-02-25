using System.Linq;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS.Extensions
{
    public static class TermSnapshotExtensions
    {
        public static string GetNextCandidate(this TermSnapshot termSnapshot, Miners currentMiners)
        {
            var ranking = termSnapshot.CandidatesSnapshot.OrderBy(ms => ms.Votes).Select(ms => ms.PublicKey).ToList();
            for (var i = Config.GetProducerNumber() + 1; i < ranking.Count; i++)
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