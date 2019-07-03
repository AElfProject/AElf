using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    internal partial class MinerList
    {
        public Consensus.AEDPoS.Round GenerateFirstRoundOfNewTerm(int miningInterval,
            Timestamp currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
        {
            var sortedMiners =
                (from obj in Pubkeys
                        .ToDictionary<ByteString, string, int>(miner => miner.ToHex(), miner => miner[0])
                    orderby obj.Value descending
                    select obj.Key).ToList();

            var round = new Consensus.AEDPoS.Round();

            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();

                // The first miner will be the extra block producer of first round of each term.
                if (i == 0)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.Pubkey = sortedMiners[i];
                minerInRound.Order = i + 1;
                minerInRound.ExpectedMiningTime = currentBlockTime.AddMilliseconds((i * miningInterval) + miningInterval);
                // Should be careful during validation.
                minerInRound.PreviousInValue = Hash.Empty;

                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }

            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;

            return round;
        }

        public Hash GetMinersHash()
        {
            var orderedMiners = Pubkeys.OrderBy(p => p);
            return Hash.FromString(orderedMiners.Aggregate("", (current, publicKey) => current + publicKey));
        }
    }
}