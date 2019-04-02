using System;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract : ConsensusContractContainer.ConsensusContractBase
    {
        public override ConsensusCommand GetConsensusCommand(CommandInput input)
        {
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var behaviour = GetBehaviour(input.PublicKey.ToHex(), Context.CurrentBlockTime, out var currentRound);
            Context.LogDebug(() => currentRound.GetLogs(input.PublicKey.ToHex(), behaviour));
            return behaviour.GetConsensusCommand(currentRound, input.PublicKey.ToHex(), Context.CurrentBlockTime);
        }

        /// <summary>
        /// Get next consensus behaviour of the caller based on current state.
        /// This method can be tested by testing GetConsensusCommand.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="dateTime"></param>
        /// <param name="currentRound">Return current round information to avoid unnecessary database access.</param>
        /// <returns></returns>
        private DPoSBehaviour GetBehaviour(string publicKey, DateTime dateTime, out Round currentRound)
        {
            currentRound = null;

            if (!TryToGetCurrentRoundInformation(out currentRound))
            {
                // This chain not initialized yet.
                return DPoSBehaviour.ChainNotInitialized;
            }

            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                // Provided public key isn't a miner.
                return DPoSBehaviour.Watch;
            }

            var isTimeSlotPassed = currentRound.IsTimeSlotPassed(publicKey, dateTime, out var minerInRound);
            var ableToGetPreviousRound = TryToGetPreviousRoundInformation(out var previousRound);
            var isTermJustChanged = IsJustChangedTerm(out var termNumber);
            if (minerInRound.OutValue == null)
            {
                if (!ableToGetPreviousRound && minerInRound.Order != 1 &&
                    currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
                {
                    return DPoSBehaviour.NextRound;
                }

                if (!ableToGetPreviousRound || isTermJustChanged)
                {
                    // Failed to get previous round information or just changed term.
                    return DPoSBehaviour.UpdateValueWithoutPreviousInValue;
                }

                if (!isTimeSlotPassed)
                {
                    // If this node not missed his time slot of current round.
                    return DPoSBehaviour.UpdateValue;
                }
            }

            if (currentRound.RoundNumber == 1)
            {
                return DPoSBehaviour.NextRound;
            }

            // If this node missed his time slot, a command of terminating current round will be fired,
            // and the terminate time will based on the order of this node (to avoid conflicts).

            Assert(TryToGetBlockchainStartTimestamp(out var blockchainStartTimestamp),
                "Failed to get blockchain start timestamp.");

            Context.LogDebug(() => $"Using start timestamp: {blockchainStartTimestamp}");
            // Calculate the approvals and make the judgement of changing term.
            return currentRound.IsTimeToChangeTerm(previousRound, blockchainStartTimestamp.ToDateTime(), termNumber)
                ? DPoSBehaviour.NextTerm
                : DPoSBehaviour.NextRound;
        }
    }
}