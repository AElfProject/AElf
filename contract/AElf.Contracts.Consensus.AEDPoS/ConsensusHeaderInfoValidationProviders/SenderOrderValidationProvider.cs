using System.Linq;
using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class SenderOrderValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Is sender's order of next round correct?
            // Miners that have determined the order of the next round should be equal to
            // miners that mined blocks during current round.
            var validationResult = new ValidationResult();
            var providedRound = validationContext.ProvidedRound;
            if (providedRound.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() != providedRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                validationResult.Message = "Invalid FinalOrderOfNextRound.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}