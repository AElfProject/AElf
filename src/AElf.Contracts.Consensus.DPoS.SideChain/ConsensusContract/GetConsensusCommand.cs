using System;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract : ConsensusContractContainer.ConsensusContractBase
    {
        public override ConsensusCommand GetConsensusCommand(CommandInput input)
        {
            Assert(input.PublicKey.Any(), "Invalid public key.");

            var behaviour = GetBehaviour(input.PublicKey.ToHex(), Context.CurrentBlockTime, out var currentRound);

            if (behaviour == DPoSBehaviour.Nothing)
            {
                return new ConsensusCommand
                {
                    ExpectedMiningTime = DateTime.MaxValue.ToUniversalTime().ToTimestamp(),
                    Hint = ByteString.CopyFrom(new DPoSHint {Behaviour = behaviour}.ToByteArray()),
                    LimitMillisecondsOfMiningBlock = int.MaxValue, NextBlockMiningLeftMilliseconds = int.MaxValue
                };
            }

            Assert(currentRound != null && currentRound.RoundId != 0, "Consensus not initialized.");

            var command = behaviour.GetConsensusCommand(currentRound, input.PublicKey.ToHex(), Context.CurrentBlockTime,
                false);

            Context.LogDebug(() =>
                currentRound.GetLogs(input.PublicKey.ToHex(), DPoSHint.Parser.ParseFrom(command.Hint).Behaviour));

            return command;
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

            if (!TryToGetCurrentRoundInformation(out currentRound) ||
                !currentRound.RealTimeMinersInformation.ContainsKey(publicKey))
            {
                LogVerbose("Consensus information not initialized or this node isn't a miner.");
                return DPoSBehaviour.Nothing;
            }

            var ableToGetPreviousRound = TryToGetPreviousRoundInformation(out var previousRound);
            var isTimeSlotPassed = currentRound.IsTimeSlotPassed(publicKey, dateTime, out var minerInRound);
            if (minerInRound.OutValue == null)
            {
                // Current miner hasn't produce block in current round before.

                if (!ableToGetPreviousRound && minerInRound.Order != 1 &&
                    currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
                {
                    // In first round, if block of boot node not executed, don't produce block to
                    // avoid forks creating.
                    LogVerbose("Will wait to produce extra block because first block of boot miner didn't executed.");
                    return DPoSBehaviour.NextRound;
                }

                if (!ableToGetPreviousRound)
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

            return DPoSBehaviour.NextRound;
        }
    }
}