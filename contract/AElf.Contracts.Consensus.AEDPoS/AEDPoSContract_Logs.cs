using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private void LogIfPreviousMinerHasNotProduceEnoughTinyBlocks(Round currentRound, string publicKey)
        {
            var minerInRound = currentRound.RealTimeMinersInformation[publicKey];

            var extraBlockProducerOfPreviousRound = currentRound.ExtraBlockProducerOfPreviousRound;

            if (extraBlockProducerOfPreviousRound == string.Empty)
            {
                return;
            }

            if (minerInRound.Order < 2)
            {
                var extraBlockProducerTinyBlocks = currentRound
                    .RealTimeMinersInformation[extraBlockProducerOfPreviousRound].ProducedTinyBlocks;
                if (extraBlockProducerTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber)
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
                 previousTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber.Mul(2)) ||
                (extraBlockProducerOfPreviousRound != previousMinerInRound.Pubkey &&
                 previousTinyBlocks < AEDPoSContractConstants.TinyBlocksNumber))
            {
                Context.LogDebug(() =>
                    $"CONSENSUS WARNING: Previous miner {previousMinerInRound.Pubkey} only produced {previousTinyBlocks} tiny blocks during round {currentRound.RoundNumber}.");
            }
        }
    }
}
