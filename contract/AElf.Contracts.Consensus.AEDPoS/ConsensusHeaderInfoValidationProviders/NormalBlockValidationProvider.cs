using Acs4;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class NormalBlockValidationProvider : IHeaderInformationValidationProvider
    {
        public ValidationResult ValidateHeaderInformation(ConsensusValidationContext validationContext)
        {
            // Need to check round id when updating current round information.
            if (!IsRoundIdMatched(validationContext))
            {
                return new ValidationResult {Message = "Round Id not match."};
            }

            if (!ValidateProducedBlocksCount(validationContext))
            {
                return new ValidationResult {Message = "Incorrect produced blocks count."};
            }

            return new ValidationResult {Success = true};
        }

        private bool IsRoundIdMatched(ConsensusValidationContext validationContext)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound, validationContext))
            {
                return currentRound.RoundId == validationContext.ProvidedRound.RoundIdForValidation;
            }

            return false;
        }

        private bool ValidateProducedBlocksCount(ConsensusValidationContext validationContext)
        {
            var pubkey = validationContext.SenderPubkey;
            return validationContext.BaseRound.RealTimeMinersInformation[pubkey].ProducedBlocks.Add(1) ==
                   validationContext.ProvidedRound.RealTimeMinersInformation[pubkey].ProducedBlocks;
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