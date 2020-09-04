using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS4;
using AElf.Sdk.CSharp;
using AElf.Types;

// ReSharper disable once CheckNamespace
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

            return new ValidationResult {Success = true};
        }

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        private bool NewConsensusInformationFilled(ConsensusValidationContext validationContext)
        {
            var minerInRound =
                validationContext.ProvidedRound.RealTimeMinersInformation[validationContext.SenderPubkey];
            return minerInRound.OutValue != null && minerInRound.Signature != null &&
                   minerInRound.OutValue.Value.Any() && minerInRound.Signature.Value.Any();
        }

        private bool ValidatePreviousInValue(ConsensusValidationContext validationContext)
        {
            var extraData = validationContext.ExtraData;
            var publicKey = validationContext.SenderPubkey;

            if (!validationContext.PreviousRound.RealTimeMinersInformation.ContainsKey(publicKey)) return true;

            if (extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue == null) return true;

            var previousOutValue = validationContext.PreviousRound.RealTimeMinersInformation[publicKey].OutValue;
            var previousInValue = extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue;
            if (previousInValue == Hash.Empty) return true;

            return HashHelper.ComputeFrom(previousInValue) == previousOutValue;
        }
    }
}