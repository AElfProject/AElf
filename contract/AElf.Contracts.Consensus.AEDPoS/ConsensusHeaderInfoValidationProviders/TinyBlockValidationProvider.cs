using Acs4;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class TinyBlockValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            if (!ValidateProducedTinyBlocksCount(validationContext))
            {
                return new ValidationResult {Message = "Incorrect produced tiny blocks count."};
            }

            return new ValidationResult {Success = true};
        }

        private bool ValidateProducedTinyBlocksCount(ConsensusValidationContext validationContext)
        {
            var pubkey = validationContext.SenderPubkey;

            return validationContext.BaseRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks.Add(1) ==
                   validationContext.ProvidedRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks;
        }
    }
}