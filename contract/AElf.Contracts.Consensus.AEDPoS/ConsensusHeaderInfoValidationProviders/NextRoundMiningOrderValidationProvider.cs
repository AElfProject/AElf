using System.Linq;
using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class NextRoundMiningOrderValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Miners that have determined the order of the next round should be equal to
            // miners that mined blocks during current round.
            var validationResult = new ValidationResult();
            var providedRound = validationContext.ProvidedRound;
            var distinctCount = providedRound.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0)
                .Distinct().Count();
            if (distinctCount != providedRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                validationResult.Message = "Invalid FinalOrderOfNextRound.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}