using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class ContinuousBlocksValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Is sender produce too many continuous blocks?
            var validationResult = new ValidationResult();

            if (validationContext.ProvidedRound.RoundNumber > 2 && // Skip first two rounds.
                validationContext.BaseRound.RealTimeMinersInformation.Count != 1)
            {
                var latestPubkeyToTinyBlocksCount = validationContext.LatestPubkeyToTinyBlocksCount;
                if (latestPubkeyToTinyBlocksCount != null &&
                    latestPubkeyToTinyBlocksCount.Pubkey == validationContext.SenderPubkey &&
                    latestPubkeyToTinyBlocksCount.BlocksCount < 0)
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