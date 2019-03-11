using System;
using System.Linq;
using AElf.Common;

namespace AElf.Consensus.DPoS
{
    public static class TermSnapshotExtensions
    {
        public static string GetNextCandidate(this TermSnapshot termSnapshot, Miners currentMiners)
        {
            var ranking = termSnapshot.CandidatesSnapshot.OrderBy(ms => ms.Votes).Select(ms => ms.PublicKey).ToList();
            for (var i = GetProducerNumber() + 1; i < ranking.Count; i++)
            {
                if (!currentMiners.PublicKeys.Contains(ranking[i]))
                {
                    return ranking[i];
                }
            }

            return "";
        }

        private static int GetProducerNumber()
        {
            return 17 + (DateTime.UtcNow.Year - 2019) * 2;
        }
    }
}