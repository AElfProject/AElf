using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class RoundTimeSlotsValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            // If provided round is a new round
            if (validationContext.ProvidedRound.RoundId != validationContext.BaseRound.RoundId)
            {
                // Is round information fits time slot rule?
                validationResult = validationContext.ProvidedRound.CheckRoundTimeSlots();
                if (!validationResult.Success)
                {
                    return validationResult;
                }
            }
            else
            {
                // Is sender respect his time slot?
                // It is maybe failing due to using too much time producing previous tiny blocks.
                if (!CheckMinerTimeSlot(validationContext))
                {
                    validationResult.Message = "Time slot already passed before execution.";
                    return validationResult;
                }
            }

            validationResult.Success = true;
            return validationResult;
        }

        private bool CheckMinerTimeSlot(ConsensusValidationContext validationContext)
        {
            var round = validationContext.ProvidedRound;
            var pubkey = validationContext.Pubkey;

            if (IsFirstRoundOfCurrentTerm(out _, validationContext)) return true;
            var minerInRound = round.RealTimeMinersInformation[pubkey];
            var latestActualMiningTime = minerInRound.ActualMiningTimes.OrderBy(t => t).LastOrDefault();
            if (latestActualMiningTime == null) return true;
            var expectedMiningTime = minerInRound.ExpectedMiningTime;
            var endOfExpectedTimeSlot = expectedMiningTime.AddMilliseconds(round.GetMiningInterval());
            if (latestActualMiningTime < expectedMiningTime)
            {
                // Which means this miner is producing tiny blocks for previous extra block slot.
                return latestActualMiningTime < round.GetStartTime();
            }

            return latestActualMiningTime < endOfExpectedTimeSlot;
        }

        private bool IsFirstRoundOfCurrentTerm(out long termNumber, ConsensusValidationContext validationContext)
        {
            termNumber = 1;
            return TryToGetTermNumber(out termNumber, validationContext.CurrentTermNumber) &&
                   TryToGetPreviousRoundInformation(out var previousRound, validationContext) &&
                   previousRound.TermNumber != termNumber ||
                   TryToGetRoundNumber(out var roundNumber, validationContext.CurrentRoundNumber) && roundNumber == 1;
        }

        private bool TryToGetTermNumber(out long termNumber, long currentTermNumber)
        {
            termNumber = currentTermNumber;
            return termNumber != 0;
        }

        private bool TryToGetRoundNumber(out long roundNumber, long currentRoundNumber)
        {
            roundNumber = currentRoundNumber;
            return roundNumber != 0;
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