using Acs4;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class RoundAndTermValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            var extraData = validationContext.ExtraData;
            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return ValidationForUpdateValue(validationContext);
                case AElfConsensusBehaviour.NextRound:
                    return ValidationForNextRound(validationContext);
                case AElfConsensusBehaviour.NextTerm:
                    return ValidationForNextTerm(validationContext);
                default:
                    validationResult.Success = true;
                    return validationResult;
            }
        }

        private ValidationResult ValidationForUpdateValue(ConsensusValidationContext validationContext)
        {
            // Need to check round id when updating current round information.
            if (!IsRoundIdMatched(validationContext))
            {
                return new ValidationResult {Message = "Round Id not match."};
            }

            // Only one Out Value should be filled.
            if (!NewOutValueFilled(validationContext))
            {
                return new ValidationResult {Message = "Incorrect new Out Value."};
            }

            if (!ValidatePreviousInValue(validationContext))
            {
                return new ValidationResult {Message = "Incorrect previous in value."};
            }

            return new ValidationResult {Success = true};
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

        private bool IsRoundIdMatched(ConsensusValidationContext validationContext)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext))
            {
                return currentRound.RoundId == validationContext.ProvidedRound.RoundId;
            }

            return false;
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

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="minersInformation"></param>
        /// <returns></returns>
        private bool NewOutValueFilled(ConsensusValidationContext validationContext)
        {
            IEnumerable<MinerInRound> minersInformation =
                validationContext.ExtraData.Round.RealTimeMinersInformation.Values;
            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext))
            {
                return currentRound.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
                       minersInformation.Count(info => info.OutValue != null);
            }

            return false;
        }

        private bool ValidatePreviousInValue(ConsensusValidationContext validationContext)
        {
            var extraData = validationContext.ExtraData;
            var publicKey = extraData.SenderPubkey.ToHex();

            if (!TryToGetPreviousRoundInformation(out var previousRound, validationContext)) return true;

            if (!previousRound.RealTimeMinersInformation.ContainsKey(publicKey)) return true;

            if (extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue == null) return true;

            var previousOutValue = previousRound.RealTimeMinersInformation[publicKey].OutValue;
            var previousInValue = extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue;
            if (previousInValue == Hash.Empty) return true;

            return Hash.FromMessage(previousInValue) == previousOutValue;
        }

        private bool TryToGetPreviousRoundInformation(out Round previousRound,
            ConsensusValidationContext validationContext, bool useCache = false)
        {
            previousRound = new Round();
            var _rounds = validationContext.RoundsDict;

            if (!TryToGetRoundNumber(out var roundNumber, validationContext.CurrentRoundNumber)) return false;
            if (roundNumber < 2) return false;
            var targetRoundNumber = roundNumber.Sub(1);
            if (useCache && _rounds.ContainsKey(targetRoundNumber))
            {
                previousRound = _rounds[targetRoundNumber];
            }
            else
            {
                previousRound = validationContext.Rounds[targetRoundNumber];
            }

            return !previousRound.IsEmpty;
        }
    }
}