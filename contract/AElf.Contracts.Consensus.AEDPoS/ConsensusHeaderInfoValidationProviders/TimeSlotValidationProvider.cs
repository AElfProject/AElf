using System.Linq;
using AElf.Standards.ACS4;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    public class TimeSlotValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            var validationResult = new ValidationResult();
            // If provided round is a new round
            if (validationContext.ProvidedRound.RoundId != validationContext.BaseRound.RoundId)
            {
                // Is new round information fits time slot rule?
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
                    validationResult.Message = $"Time slot already passed before execution.{validationContext.SenderPubkey}";
                    validationResult.IsReTrigger = true;
                    return validationResult;
                }
            }

            validationResult.Success = true;
            return validationResult;
        }

        private bool CheckMinerTimeSlot(ConsensusValidationContext validationContext)
        {
            if (IsFirstRoundOfCurrentTerm(out _, validationContext)) return true;
            var minerInRound = validationContext.BaseRound.RealTimeMinersInformation[validationContext.SenderPubkey];
            var latestActualMiningTime = minerInRound.ActualMiningTimes.OrderBy(t => t).LastOrDefault();
            if (latestActualMiningTime == null) return true;
            var expectedMiningTime = minerInRound.ExpectedMiningTime;
            var endOfExpectedTimeSlot =
                expectedMiningTime.AddMilliseconds(validationContext.BaseRound.GetMiningInterval());
            if (latestActualMiningTime < expectedMiningTime)
            {
                // Which means this miner is producing tiny blocks for previous extra block slot.
                return latestActualMiningTime < validationContext.BaseRound.GetRoundStartTime();
            }

            return latestActualMiningTime < endOfExpectedTimeSlot;
        }

        private bool IsFirstRoundOfCurrentTerm(out long termNumber, ConsensusValidationContext validationContext)
        {
            termNumber = validationContext.CurrentTermNumber;
            return validationContext.PreviousRound.TermNumber != termNumber ||
                   validationContext.CurrentRoundNumber == 1;
        }
    }
}