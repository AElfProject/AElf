using Acs4;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class RoundTerminateValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var extraData = validationContext.ExtraData;
            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.NextRound:
                    return ValidationForNextRound(validationContext);
                case AElfConsensusBehaviour.NextTerm:
                    return ValidationForNextTerm(validationContext);
                default:
                    validationResult.Success = true;
                    return validationResult;
            }
        }

        private ValidationResult ValidationForNextRound(ConsensusValidationContext validationContext)
        {
            // Is next round information correct?
            var extraData = validationContext.ExtraData;
            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext, true) &&
                currentRound.RoundNumber.Add(1) != extraData.Round.RoundNumber)
            {
                return new ValidationResult {Message = "Incorrect round number for next round."};
            }

            if (extraData.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
            {
                return new ValidationResult {Message = "Incorrect next round information."};
            }


            return new ValidationResult {Success = true};
        }

        private ValidationResult ValidationForNextTerm(ConsensusValidationContext validationContext)
        {
            // Is next round information correct?
            var extraData = validationContext.ExtraData;
            var validationResult = ValidationForNextRound(validationContext);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext, true) &&
                currentRound.TermNumber.Add(1) != extraData.Round.TermNumber)
            {
                return new ValidationResult {Message = "Incorrect term number for next round."};
            }

            return new ValidationResult {Success = true};
        }

        private bool TryToGetCurrentRoundInformation(out Round round, ConsensusValidationContext validationContext,
            bool useCache = false)
        {
            round = null;
            var rounds = validationContext.RoundsDict;
            if (!TryToGetRoundNumber(out var roundNumber, validationContext.CurrentRoundNumber)) return false;

            if (useCache && rounds.ContainsKey(roundNumber))
            {
                round = rounds[roundNumber];
            }
            else
            {
                round = validationContext.Rounds[roundNumber];
            }

            return !round.IsEmpty;
        }

        private bool TryToGetRoundNumber(out long roundNumber, long currentRoundNumber)
        {
            roundNumber = currentRoundNumber;
            return roundNumber != 0;
        }

    }
}