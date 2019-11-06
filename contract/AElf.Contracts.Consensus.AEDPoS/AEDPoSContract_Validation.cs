using System.Collections.Generic;
using Acs4;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
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
            // This validation focuses on the new round information.

            // According to current round information:
            if (!TryToGetCurrentRoundInformation(out var baseRound))
            {
                return new ValidationResult {Success = false, Message = "Failed to get current round information."};
            }

            /* Ask several questions: */

            var validationContext = new ConsensusValidationContext
            {
                BaseRound = baseRound,
                CurrentTermNumber = State.CurrentTermNumber.Value,
                CurrentRoundNumber = State.CurrentRoundNumber.Value,
                Rounds = State.Rounds,
                LatestProviderToTinyBlocksCount = State.LatestProviderToTinyBlocksCount.Value,
                ExtraData = extraData,
                RoundsDict = _rounds
            };
            var service = new HeaderInformationValidationService(new List<IHeaderInformationValidationProvider>
            {
                new MiningPermissionValidationProvider(),
                new RoundTimeSlotsValidationProvider(),
                new ContinuousBlocksValidationProvider(),
                new SenderOrderValidationProvider(),
                new ConfirmedLibValidationProvider(),
                new RoundAndTermValidationProvider()
            });

            var validationResult = service.ValidateInformation(validationContext);
            if (validationResult.Success == false)
            {
                Context.LogDebug(() => $" Validate failed : {validationResult.Message}");
            }

            return validationResult;
        }
    }
}