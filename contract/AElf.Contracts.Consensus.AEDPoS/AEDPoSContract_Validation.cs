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
            // According to current round information:
            if (!TryToGetCurrentRoundInformation(out var baseRound))
            {
                return new ValidationResult {Success = false, Message = "Failed to get current round information."};
            }

            var providedRound = extraData.Round;
            if (extraData.Behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                providedRound = baseRound.RecoverFromUpdateValue(extraData.Round, extraData.SenderPubkey.ToHex());
            }

            if (extraData.Behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                providedRound = baseRound.RecoverFromTinyBlock(extraData.Round, extraData.SenderPubkey.ToHex());
            }

            var validationContext = new ConsensusValidationContext
            {
                BaseRound = baseRound,
                CurrentTermNumber = State.CurrentTermNumber.Value,
                CurrentRoundNumber = State.CurrentRoundNumber.Value,
                Rounds = State.Rounds,
                LatestProviderToTinyBlocksCount = State.LatestProviderToTinyBlocksCount.Value,
                ExtraData = extraData,
                RoundsDict = _rounds,
                ProvidedRound = providedRound
            };

            /* Ask several questions: */

            // Add basic providers at first.
            var validationProviders = new List<IHeaderInformationValidationProvider>
            {
                // Is sender in miner list?
                new MiningPermissionValidationProvider(),

                // Is this block produced in proper time?
                new TimeSlotValidationProvider(),

                // Is sender produced too many blocks at one time?
                new ContinuousBlocksValidationProvider(),

                // Is confirmed lib height and lib round number went down? (Which should not happens.)
                new LibInformationValidationProvider(),
            };

            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValue:
                    validationProviders.Add(new UpdateValueValidationProvider());
                    break;
                case AElfConsensusBehaviour.TinyBlock:
                    //validationProviders.Add(new TinyBlockValidationProvider());
                    break;
                case AElfConsensusBehaviour.NextRound:
                    // Is sender's order of next round correct?
                    validationProviders.Add(new NextRoundMiningOrderValidationProvider());
                    validationProviders.Add(new RoundTerminateValidationProvider());
                    break;
                case AElfConsensusBehaviour.NextTerm:
                    validationProviders.Add(new RoundTerminateValidationProvider());
                    break;
            }

            var service = new HeaderInformationValidationService(validationProviders);

            Context.LogDebug(() => $"Validating behaviour: {extraData.Behaviour.ToString()}");

            var validationResult = service.ValidateInformation(validationContext);
            if (validationResult.Success == false)
            {
                Context.LogDebug(() => $"Consensus Validation before execution failed : {validationResult.Message}");
            }

            return validationResult;
        }
    }
}