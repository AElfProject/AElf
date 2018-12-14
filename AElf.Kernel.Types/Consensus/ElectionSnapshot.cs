using System.Linq;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class ElectionSnapshot
    {
        public byte[] GetNextCandidate(Miners currentMiners)
        {
            var ranking = MinersSnapshot.OrderBy(ms => ms.VotersWeights).Select(ms => ms.MinerPubKey).ToList();
            for (var i = GlobalConfig.BlockProducerNumber + 1; i < ranking.Count(); i++)
            {
                if (!currentMiners.Producers.Contains(ranking[i]))
                {
                    return ranking[i].ToByteArray();
                }
            }

            return new byte[0];
        }
    }
}