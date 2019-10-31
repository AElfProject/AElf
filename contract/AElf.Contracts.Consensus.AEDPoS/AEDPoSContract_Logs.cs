using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private void LogIfPreviousMinerHasNotProduceEnoughTinyBlocks(Round currentRound, string pubkey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(pubkey))
            {
                return;
            }

            var minerInRound = currentRound.RealTimeMinersInformation[pubkey];

            var extraBlockProducerOfPreviousRound = currentRound.ExtraBlockProducerOfPreviousRound;

            if (extraBlockProducerOfPreviousRound == string.Empty)
            {
                return;
            }

            if (minerInRound.Order < 2)
            {
                if (!currentRound.RealTimeMinersInformation.ContainsKey(extraBlockProducerOfPreviousRound))
                {
                    return;
                }
                var extraBlockProducerTinyBlocks = currentRound
                    .RealTimeMinersInformation[extraBlockProducerOfPreviousRound].ProducedTinyBlocks;
                if (extraBlockProducerTinyBlocks < AEDPoSContractConstants.MaximumTinyBlocksCount)
                {
                    Context.LogDebug(() =>
                        $"CONSENSUS WARNING: Previous extra block miner {extraBlockProducerOfPreviousRound} only produced {extraBlockProducerTinyBlocks} tiny blocks during round {currentRound.RoundNumber}.");
                }

                return;
            }

            var previousMinerInRound =
                currentRound.RealTimeMinersInformation.Values.First(m => m.Order == minerInRound.Order.Sub(1));
            var previousTinyBlocks = previousMinerInRound.ProducedTinyBlocks;
            if ((extraBlockProducerOfPreviousRound == previousMinerInRound.Pubkey &&
                 previousTinyBlocks < AEDPoSContractConstants.MaximumTinyBlocksCount.Mul(2)) ||
                (extraBlockProducerOfPreviousRound != previousMinerInRound.Pubkey &&
                 previousTinyBlocks < AEDPoSContractConstants.MaximumTinyBlocksCount))
            {
                Context.LogDebug(() =>
                    $"CONSENSUS WARNING: Previous miner {previousMinerInRound.Pubkey} only produced {previousTinyBlocks} tiny blocks during round {currentRound.RoundNumber}.");
            }
        }
    }
}
