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
        /// <summary>
        /// This method will be executed before executing a block.
        /// </summary>
        /// <param name="extraData"></param>
        /// <returns></returns>
        private ValidationResult ValidateBeforeExecution(AElfConsensusHeaderInformation extraData)
        {
            // We can trust this because we already validated the pubkey
            // during `AEDPoSExtraDataExtractor.ExtractConsensusExtraData`
            var pubkey = extraData.SenderPubkey.ToHex();
            
            // This validation focuses on the new round information.
            var providedRound = extraData.Round;

            // According to current round information:
            if (!TryToGetCurrentRoundInformation(out var baseRound))
            {
                return new ValidationResult {Success = false, Message = "Failed to get current round information."};
            }

            /* Ask several questions: */

            // Is sender in miner list?
            if (!baseRound.IsInMinerList(pubkey))
            {
                Context.LogDebug(() => "Sender is not a miner.");
                return new ValidationResult {Success = false, Message = $"Sender {pubkey} is not a miner."};
            }

            // If provided round is a new round
            if (providedRound.RoundId != baseRound.RoundId)
            {
                // Is round information fits time slot rule?
                var timeSlotsCheckResult = providedRound.CheckRoundTimeSlots();
                if (!timeSlotsCheckResult.Success)
                {
                    Context.LogDebug(() => $"Round time slots incorrect: {timeSlotsCheckResult.Message}");
                    return timeSlotsCheckResult;
                }
            }
            else
            {
                // Is sender respect his time slot?
                // It is maybe failing due to using too much time producing previous tiny blocks.
                if (!CheckMinerTimeSlot(providedRound, pubkey))
                {
                    Context.LogDebug(() => "Time slot already passed before execution.");
                    return new ValidationResult {Message = "Time slot already passed before execution."};
                }
            }

            // Is sender produce too many continuous blocks?
            // Skip first two rounds.
            if (providedRound.RoundNumber > 2 && baseRound.RealTimeMinersInformation.Count != 1)
            {
                var latestProviderToTinyBlocksCount = State.LatestProviderToTinyBlocksCount.Value;
                if (latestProviderToTinyBlocksCount != null && latestProviderToTinyBlocksCount.Pubkey == pubkey &&
                    latestProviderToTinyBlocksCount.BlocksCount < 0)
                {
                    Context.LogDebug(() => $"Sender {pubkey} produced too many continuous blocks.");
                    return new ValidationResult {Message = "Sender produced too many continuous blocks."};
                }
            }

            // Is sender's order of next round correct?
            // Miners that have determined the order of the next round should be equal to
            // miners that mined blocks during current round.
            if (providedRound.RealTimeMinersInformation.Values.Where(m => m.FinalOrderOfNextRound > 0).Distinct()
                    .Count() != providedRound.RealTimeMinersInformation.Values.Count(m => m.OutValue != null))
            {
                return new ValidationResult {Message = "Invalid FinalOrderOfNextRound."};
            }

            // Is confirmed lib height and lib round number went down?
            if (baseRound.ConfirmedIrreversibleBlockHeight > providedRound.ConfirmedIrreversibleBlockHeight ||
                baseRound.ConfirmedIrreversibleBlockRoundNumber > providedRound.ConfirmedIrreversibleBlockRoundNumber)
            {
                return new ValidationResult {Message = "Incorrect confirmed lib information."};
            }

            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue:
                case AElfConsensusBehaviour.UpdateValue:
                    return ValidationForUpdateValue(extraData);
                case AElfConsensusBehaviour.NextRound:
                    return ValidationForNextRound(extraData);
                case AElfConsensusBehaviour.NextTerm:
                    return ValidationForNextTerm(extraData);
            }

            return new ValidationResult {Success = true};
        }

        private bool CheckMinerTimeSlot(Round round, string publicKey)
        {
            if (IsFirstRoundOfCurrentTerm(out _)) return true;
            var minerInRound = round.RealTimeMinersInformation[publicKey];
            var latestActualMiningTime = minerInRound.ActualMiningTimes.OrderBy(t => t).LastOrDefault();
            if (latestActualMiningTime == null) return true;
            var expectedMiningTime = minerInRound.ExpectedMiningTime;
            var endOfExpectedTimeSlot = expectedMiningTime.AddMilliseconds(round.GetMiningInterval());
            if (latestActualMiningTime < expectedMiningTime)
            {
                // Which means this miner is producing tiny blocks for previous extra block slot.
                Context.LogDebug(() =>
                    $"latest actual mining time: {latestActualMiningTime}, round start time: {round.GetStartTime()}");
                return latestActualMiningTime < round.GetStartTime();
            }

            Context.LogDebug(() =>
                $"latest actual mining time: {latestActualMiningTime}, end of expected mining time: {endOfExpectedTimeSlot}");
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
            // Is next round information correct?
            if (TryToGetCurrentRoundInformation(out var currentRound, true) &&
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

        private ValidationResult ValidationForNextTerm(AElfConsensusHeaderInformation extraData)
        {
            // Is next round information correct?
            var validationResult = ValidationForNextRound(extraData);
            if (!validationResult.Success)
            {
                return validationResult;
            }
            if (TryToGetCurrentRoundInformation(out var currentRound, true) &&
                currentRound.TermNumber.Add(1) != extraData.Round.TermNumber)
            {
                return new ValidationResult {Message = "Incorrect term number for next round."};
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
            var publicKey = extraData.SenderPubkey.ToHex();

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