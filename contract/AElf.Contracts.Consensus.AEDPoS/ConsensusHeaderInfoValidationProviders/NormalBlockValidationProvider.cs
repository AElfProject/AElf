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
            // It's meaningless if round changed.
            if (validationContext.BaseRound.RoundNumber != validationContext.ProvidedRound.RoundNumber)
                return new ValidationResult {Success = true};

            // Need to check round id when updating current round information.
            if (!IsRoundIdMatched(validationContext))
            {
                return new ValidationResult {Message = "Round Id not match."};
            }

            if (!ValidateProducedBlocksCount(validationContext))
            {
                return new ValidationResult
                {
                    Message = "Incorrect produced blocks count."
                };
            }

            return new ValidationResult {Success = true};
        }

        private bool IsRoundIdMatched(ConsensusValidationContext validationContext)
        {
            return validationContext.BaseRound.RoundId == validationContext.ProvidedRound.RoundIdForValidation;
        }

        /// <summary>
        /// Either stay, or plus one.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private bool ValidateProducedBlocksCount(ConsensusValidationContext validationContext)
        {
            var pubkey = validationContext.SenderPubkey;
            if (!validationContext.BaseRound.RealTimeMinersInformation.ContainsKey(pubkey) ||
                !validationContext.ProvidedRound.RealTimeMinersInformation.ContainsKey(pubkey))
            {
                // If sender isn't a current miner, this validation is meaning less.
                return true;
            }
            var before = validationContext.BaseRound.RealTimeMinersInformation[pubkey];
            var after = validationContext.ProvidedRound.RealTimeMinersInformation[pubkey];
            var result = (before.ProducedBlocks == after.ProducedBlocks ||
                          before.ProducedBlocks.Add(1) == after.ProducedBlocks) &&
                         (before.ProducedTinyBlocks == after.ProducedTinyBlocks ||
                          before.ProducedTinyBlocks.Add(1) == after.ProducedTinyBlocks);
            return result;
        }
    }
}