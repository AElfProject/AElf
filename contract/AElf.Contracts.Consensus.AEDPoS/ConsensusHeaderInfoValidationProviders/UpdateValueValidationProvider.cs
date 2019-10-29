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


        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="validationContext"></param>
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


        private bool IsRoundIdMatched(ConsensusValidationContext validationContext)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext))
            {
                return currentRound.RoundId == validationContext.ProvidedRound.RoundId;
            }

            return false;
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