using AElf.Standards.ACS4;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using AElf.Sdk.CSharp;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class RoundTerminateValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var extraData = validationContext.ExtraData;
            if (extraData.Behaviour == AElfConsensusBehaviour.NextRound)
            {
                return ValidationForNextRound(validationContext);
            }

            if (extraData.Behaviour == AElfConsensusBehaviour.NextTerm)
            {
                return ValidationForNextTerm(validationContext);
            }

            validationResult.Success = true;
            return validationResult;
        }

        private ValidationResult ValidationForNextRound(ConsensusValidationContext validationContext)
        {
            // Is next round information correct?
            // Currently two aspects:
            //   Round Number
            //   In Values Should Be Null
            var extraData = validationContext.ExtraData;
            if (validationContext.BaseRound.RoundNumber.Add(1) != extraData.Round.RoundNumber)
            {
                return new ValidationResult {Message = "Incorrect round number for next round."};
            }

            return extraData.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null)
                ? new ValidationResult {Message = "Incorrect next round information."}
                : new ValidationResult {Success = true};
        }

        private ValidationResult ValidationForNextTerm(ConsensusValidationContext validationContext)
        {
            var extraData = validationContext.ExtraData;
            var validationResult = ValidationForNextRound(validationContext);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            // Is next term number correct?
            return validationContext.BaseRound.TermNumber.Add(1) != extraData.Round.TermNumber
                ? new ValidationResult {Message = "Incorrect term number for next round."}
                : new ValidationResult {Success = true};
        }
    }
}