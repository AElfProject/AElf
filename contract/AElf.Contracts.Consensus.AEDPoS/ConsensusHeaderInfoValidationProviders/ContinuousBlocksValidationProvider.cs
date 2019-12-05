using Acs4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class ContinuousBlocksValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Is sender produce too many continuous blocks?
            // Skip first two rounds.
            var validationResult = new ValidationResult();

            if (validationContext.ProvidedRound.RoundNumber > 2 &&
                validationContext.BaseRound.RealTimeMinersInformation.Count != 1)
            {
                var latestProviderToTinyBlocksCount = validationContext.LatestPubkeyToTinyBlocksCount;
                if (latestProviderToTinyBlocksCount != null &&
                    latestProviderToTinyBlocksCount.Pubkey == validationContext.SenderPubkey &&
                    latestProviderToTinyBlocksCount.BlocksCount < 0)
                {
                    validationResult.Message = "Sender produced too many continuous blocks.";
                    return validationResult;
                }
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}