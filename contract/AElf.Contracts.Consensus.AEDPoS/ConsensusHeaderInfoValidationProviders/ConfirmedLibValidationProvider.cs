using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class ConfirmedLibValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var baseRound = validationContext.BaseRound;
            var providedRound = validationContext.ProvidedRound;
            if (baseRound.ConfirmedIrreversibleBlockHeight > providedRound.ConfirmedIrreversibleBlockHeight ||
                baseRound.ConfirmedIrreversibleBlockRoundNumber > providedRound.ConfirmedIrreversibleBlockRoundNumber)
            {
                validationResult.Message = "Incorrect confirmed lib information.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}