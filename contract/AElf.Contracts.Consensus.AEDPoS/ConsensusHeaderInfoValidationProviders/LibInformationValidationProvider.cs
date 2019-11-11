using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class LibInformationValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var baseRound = validationContext.BaseRound;
            var providedRound = validationContext.ProvidedRound;
            var pubkey = validationContext.Pubkey;
            if (baseRound.ConfirmedIrreversibleBlockHeight > providedRound.ConfirmedIrreversibleBlockHeight ||
                baseRound.ConfirmedIrreversibleBlockRoundNumber > providedRound.ConfirmedIrreversibleBlockRoundNumber ||
                (providedRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight != 0 &&
                 baseRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight >
                 providedRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight))
            {
                validationResult.Message = "Incorrect lib information.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}