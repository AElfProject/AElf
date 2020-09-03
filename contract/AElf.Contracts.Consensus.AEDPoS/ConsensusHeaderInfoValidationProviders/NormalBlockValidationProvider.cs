using AElf.Standards.ACS4;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class NormalBlockValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Need to check round id when updating current round information.
            if (!IsRoundIdMatched(validationContext))
            {
                return new ValidationResult {Message = "Round Id not match."};
            }

            if (!ValidateProducedBlocksCount(validationContext))
            {
                return new ValidationResult {Message = "Incorrect produced blocks count."};
            }

            return new ValidationResult {Success = true};
        }

        private bool IsRoundIdMatched(ConsensusValidationContext validationContext)
        {
            return validationContext.BaseRound.RoundId == validationContext.ProvidedRound.RoundIdForValidation;
        }

        private bool ValidateProducedBlocksCount(ConsensusValidationContext validationContext)
        {
            var pubkey = validationContext.SenderPubkey;
            return validationContext.BaseRound.RealTimeMinersInformation[pubkey].ProducedBlocks.Add(1) ==
                   validationContext.ProvidedRound.RealTimeMinersInformation[pubkey].ProducedBlocks;
        }
    }
}