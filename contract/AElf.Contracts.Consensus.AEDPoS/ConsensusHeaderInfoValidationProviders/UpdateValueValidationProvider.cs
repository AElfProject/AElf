using System.Collections.Generic;
using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class UpdateValueValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Only one Out Value should be filled.
            if (!NewConsensusInformationFilled(validationContext))
            {
                return new ValidationResult {Message = "Incorrect new Out Value."};
            }

            if (!ValidatePreviousInValue(validationContext))
            {
                return new ValidationResult {Message = "Incorrect previous in value."};
            }

            if (!ValidateProducedTinyBlocksCount(validationContext))
            {
                return new ValidationResult {Message = "Incorrect produced tiny blocks count."};
            }

            return new ValidationResult {Success = true};
        }

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private bool NewConsensusInformationFilled(ConsensusValidationContext validationContext)
        {
            var minerInRound = validationContext.ProvidedRound.RealTimeMinersInformation[validationContext.Pubkey];
            return minerInRound.OutValue != null && minerInRound.Signature != null &&
                   minerInRound.OutValue.Value.Any() && minerInRound.Signature.Value.Any();
        }

        private bool ValidatePreviousInValue(ConsensusValidationContext validationContext)
        {
            var extraData = validationContext.ExtraData;
            var publicKey = validationContext.Pubkey;

            if (!TryToGetPreviousRoundInformation(out var previousRound, validationContext)) return true;

            if (!previousRound.RealTimeMinersInformation.ContainsKey(publicKey)) return true;

            // TODO: Fix this in secret-sharing branch.
            if (extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue == null) return true;

            var previousOutValue = previousRound.RealTimeMinersInformation[publicKey].OutValue;
            var previousInValue = extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue;
            if (previousInValue == Hash.Empty) return true;

            return Hash.FromMessage(previousInValue) == previousOutValue;
        }

        private bool ValidateProducedTinyBlocksCount(ConsensusValidationContext validationContext)
        {
            var pubkey = validationContext.Pubkey;

            if (validationContext.BaseRound.ExtraBlockProducerOfPreviousRound != pubkey)
            {
                return validationContext.ProvidedRound.RealTimeMinersInformation[pubkey].ProducedTinyBlocks == 1;
            }

            return true;
        }

        private bool TryToGetPreviousRoundInformation(out Round previousRound,
            ConsensusValidationContext validationContext, bool useCache = false)
        {
            previousRound = new Round();
            var rounds = validationContext.RoundsDict;

            if (!TryToGetRoundNumber(out var roundNumber, validationContext.CurrentRoundNumber)) return false;
            if (roundNumber < 2) return false;
            var targetRoundNumber = roundNumber.Sub(1);
            if (useCache && rounds.ContainsKey(targetRoundNumber))
            {
                previousRound = rounds[targetRoundNumber];
            }
            else
            {
                previousRound = validationContext.Rounds[targetRoundNumber];
            }

            return !previousRound.IsEmpty;
        }

        private bool TryToGetRoundNumber(out long roundNumber, long currentRoundNumber)
        {
            roundNumber = currentRoundNumber;
            return roundNumber != 0;
        }
    }
}