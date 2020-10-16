using AElf.Standards.ACS4;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class LibInformationValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var baseRound = validationContext.BaseRound;
            var providedRound = validationContext.ProvidedRound;
            var pubkey = validationContext.SenderPubkey;
            if (providedRound.ConfirmedIrreversibleBlockHeight != 0 &&
                providedRound.ConfirmedIrreversibleBlockRoundNumber != 0 &&
                (baseRound.ConfirmedIrreversibleBlockHeight > providedRound.ConfirmedIrreversibleBlockHeight ||
                 baseRound.ConfirmedIrreversibleBlockRoundNumber > providedRound.ConfirmedIrreversibleBlockRoundNumber))
            {
                validationResult.Message = "Incorrect lib information.";
                return validationResult;
            }

            if (providedRound.RealTimeMinersInformation.ContainsKey(pubkey) &&
                providedRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight != 0 &&
                baseRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight >
                providedRound.RealTimeMinersInformation[pubkey].ImpliedIrreversibleBlockHeight)
            {
                validationResult.Message = "Incorrect implied lib height.";
                return validationResult;
            }

            validationResult.Success = true;
            return validationResult;
        }
    }
}