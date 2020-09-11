using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS4;
using AElf.CSharp.Core;

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

            // Skip the certain initial miner during first several rounds. (When other nodes haven't produce blocks yet.)
            if (baseRound.RealTimeMinersInformation.Count != 1 &&
                Context.CurrentHeight < AEDPoSContractConstants.MaximumTinyBlocksCount.Mul(3))
            {
                string producedMiner = null;
                var result = true;
                for (var i = baseRound.RoundNumber; i > 0; i--)
                {
                    var producedMiners = State.Rounds[i].RealTimeMinersInformation.Values
                        .Where(m => m.ActualMiningTimes.Any()).ToList();
                    if (producedMiners.Count != 1)
                    {
                        result = false;
                        break;
                    }
                    if (producedMiner == null)
                    {
                        producedMiner = producedMiners.Single().Pubkey;
                    }
                    else if (producedMiner != producedMiners.Single().Pubkey)
                    {
                        result = false;
                    }
                }

                if (result)
                {
                    return new ValidationResult {Success = true};
                }
            }

            if (extraData.Behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                baseRound.RecoverFromUpdateValue(extraData.Round, extraData.SenderPubkey.ToHex());
            }

            if (extraData.Behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                baseRound.RecoverFromTinyBlock(extraData.Round, extraData.SenderPubkey.ToHex());
            }

            var validationContext = new ConsensusValidationContext
            {
                BaseRound = baseRound,
                CurrentTermNumber = State.CurrentTermNumber.Value,
                CurrentRoundNumber = State.CurrentRoundNumber.Value,
                PreviousRound = TryToGetPreviousRoundInformation(out var previousRound) ? previousRound : new Round(),
                LatestPubkeyToTinyBlocksCount = State.LatestPubkeyToTinyBlocksCount.Value,
                ExtraData = extraData
            };

            /* Ask several questions: */

            // Add basic providers at first.
            var validationProviders = new List<IHeaderInformationValidationProvider>
            {
                // Is sender in miner list (of base round)?
                new MiningPermissionValidationProvider(),

                // Is this block produced in proper time?
                new TimeSlotValidationProvider(),

                // Is sender produced too many blocks at one time?
                new ContinuousBlocksValidationProvider()
            };

            switch (extraData.Behaviour)
            {
                case AElfConsensusBehaviour.UpdateValue:
                    validationProviders.Add(new UpdateValueValidationProvider());
                    // Is confirmed lib height and lib round number went down? (Which should not happens.)
                    validationProviders.Add(new LibInformationValidationProvider());
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