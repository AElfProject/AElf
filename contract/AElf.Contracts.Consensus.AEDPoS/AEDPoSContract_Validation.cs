using System;
using System.Linq;
using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private ValidationResult ValidateBeforeExecution(AElfConsensusHeaderInformation extraData)
        {
            var publicKey = extraData.SenderPublicKey;

            // Validate the sender.
            if (TryToGetCurrentRoundInformation(out var currentRound) &&
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey.ToHex()))
            {
                return new ValidationResult {Success = false, Message = $"Sender {publicKey.ToHex()} is not a miner."};
            }

            // Validate the time slots.
            var timeSlotsCheckResult = extraData.Round.CheckTimeSlots();
            if (!timeSlotsCheckResult.Success)
            {
                return timeSlotsCheckResult;
            }

            var behaviour = extraData.Behaviour;

            // Try to get current round information (for further validation).
            if (currentRound == null)
            {
                return new ValidationResult
                    {Success = false, Message = "Failed to get current round information."};
            }

            if (extraData.Round.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() !=
                extraData.Round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                return new ValidationResult
                    {Success = false, Message = "Invalid FinalOrderOfNextRound."};
            }

            switch (behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    // Need to check round id when updating current round information.
                    // This can tell the miner current block 
                    if (!RoundIdMatched(extraData.Round))
                    {
                        return new ValidationResult {Success = false, Message = "Round Id not match."};
                    }

                    // Only one Out Value should be filled.
                    if (!NewOutValueFilled(extraData.Round.RealTimeMinersInformation.Values))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect new Out Value."};
                    }

                    if (!ValidatePreviousInValue(extraData))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect previous in value."};
                    }

                    break;
                case AElfConsensusBehaviour.NextRound:
                    // None of in values should be filled.
                    if (extraData.Round.RealTimeMinersInformation.Values.Any(m => m.InValue != null))
                    {
                        return new ValidationResult {Success = false, Message = "Incorrect in values."};
                    }

                    break;
                case AElfConsensusBehaviour.NextTerm:
                    break;
                case AElfConsensusBehaviour.Nothing:
                    return new ValidationResult {Success = false, Message = "Invalid behaviour"};
                case AElfConsensusBehaviour.TinyBlock:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ValidationResult {Success = true};
        }
    }
}