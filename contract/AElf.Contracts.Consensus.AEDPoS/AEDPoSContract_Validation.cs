using System;
using System.Collections.Generic;
using System.Linq;
using Acs4;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private ValidationResult ValidateBeforeExecution(AElfConsensusHeaderInformation extraData)
        {
            var publicKey = extraData.SenderPublicKey.ToHex();
            var updatedRound = extraData.Round;

            // Validate the sender.
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return new ValidationResult {Success = false, Message = $"Sender {publicKey} is not a miner."};
            }

            // Validate the time slots of round information.
            var timeSlotsCheckResult = updatedRound.CheckRoundTimeSlots();
            if (!timeSlotsCheckResult.Success)
            {
                return timeSlotsCheckResult;
            }
            
            // Validate whether this miner abide by his time slot.
            // Maybe failing due to using too much time handle things after mining.
            if (!CheckMinerTimeSlot(currentRound, publicKey))
            {
                return new ValidationResult {Message = "Time slot already passed before execution."};
            }

            if (updatedRound.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() != updatedRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                return new ValidationResult {Message = "Invalid FinalOrderOfNextRound."};
            }

            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return ValidationForUpdateValue(extraData);
                case AElfConsensusBehaviour.NextRound:
                    return ValidationForNextRound(extraData);
            }

            return new ValidationResult {Success = true};
        }

        private bool CheckMinerTimeSlot(Round round, string publicKey)
        {
            if (round.RoundNumber == 1) return true;
            var minerInRound = round.RealTimeMinersInformation[publicKey];
            var latestActualMiningTime = minerInRound.ActualMiningTimes.LastOrDefault();
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

        private ValidationResult ValidationForUpdateValue(AElfConsensusHeaderInformation extraData)
        {
            // Need to check round id when updating current round information.
            if (!IsRoundIdMatched(extraData.Round))
            {
                return new ValidationResult {Message = "Round Id not match."};
            }

            // Only one Out Value should be filled.
            if (!NewOutValueFilled(extraData.Round.RealTimeMinersInformation.Values))
            {
                return new ValidationResult {Message = "Incorrect new Out Value."};
            }

            if (!ValidatePreviousInValue(extraData))
            {
                return new ValidationResult {Message = "Incorrect previous in value."};
            }

            return new ValidationResult {Success = true};
        }

        private ValidationResult ValidationForNextRound(AElfConsensusHeaderInformation extraData)
        {
            // None of in values should be filled.
            if (extraData.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
            {
                return new ValidationResult {Message = "Incorrect in values."};
            }

            return new ValidationResult {Success = true};
        }

        private bool IsRoundIdMatched(Round round)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return currentRound.RoundId == round.RoundId;
            }

            return false;
        }

        /// <summary>
        /// Check only one Out Value was filled during this updating.
        /// </summary>
        /// <param name="minersInformation"></param>
        /// <returns></returns>
        private bool NewOutValueFilled(IEnumerable<MinerInRound> minersInformation)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                return currentRound.RealTimeMinersInformation.Values.Count(info => info.OutValue != null) + 1 ==
                       minersInformation.Count(info => info.OutValue != null);
            }

            return false;
        }
        
        private bool ValidatePreviousInValue(AElfConsensusHeaderInformation extraData)
        {
            var publicKey = extraData.SenderPublicKey.ToHex();

            if (!TryToGetPreviousRoundInformation(out var previousRound)) return true;

            if (!previousRound.RealTimeMinersInformation.ContainsKey(publicKey)) return true;

            if (extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue == null) return true;

            var previousOutValue = previousRound.RealTimeMinersInformation[publicKey].OutValue;
            var previousInValue = extraData.Round.RealTimeMinersInformation[publicKey].PreviousInValue;
            if (previousInValue == Hash.Empty) return true;

            return Hash.FromMessage(previousInValue) == previousOutValue;
        }
    }
}