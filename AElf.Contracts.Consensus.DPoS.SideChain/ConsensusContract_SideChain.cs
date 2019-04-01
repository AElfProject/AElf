using System;
using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
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
            var ableToGetPreviousRound = TryToGetPreviousRoundInformation(out _);
            if (minerInRound.OutValue == null)
            {
                if (!ableToGetPreviousRound && minerInRound.Order != 1 &&
                    currentRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).OutValue == null)
                {
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
        
        public override Empty UpdateMainChainConsensus(ConsensusInformation input)
        {
            // TODO: Only cross chain contract can call UpdateMainChainConsensus method of consensus contract.
            
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            if(input == null || input.Bytes.IsEmpty)
                return new Empty();
            var consensusInformation = DPoSHeaderInformation.Parser.ParseFrom(input.Bytes);
            
            if(consensusInformation.Round.TermNumber <= State.TermNumberFromMainChainField.Value)
                return new Empty();
            Context.LogDebug(() => $"Shared BP of term {consensusInformation.Round.TermNumber.ToInt64Value()}");
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.TermNumberFromMainChainField.Value = consensusInformation.Round.TermNumber;
            State.CurrentMiners.Value = minersKeys.ToList().ToMiners();
            return new Empty();
        }
        
        private bool GenerateNextRoundInformation(Round currentRound, DateTime dateTime,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            if (State.CurrentMiners.Value == null || currentRound.RealTimeMinersInformation.Keys.ToList().ToMiners().GetMinersHash() ==
                State.CurrentMiners.Value.GetMinersHash())
            {
                return currentRound.GenerateNextRoundInformation(dateTime, blockchainStartTimestamp, out nextRound);
            }

            nextRound = State.CurrentMiners.Value.GenerateFirstRoundOfNewTerm(currentRound.GetMiningInterval(),
                Context.CurrentBlockTime, currentRound.RoundNumber, State.TermNumberFromMainChainField.Value);
            return true;
        }

        private void InitialSettings(Round firstRound)
        {
            // Do some initializations.
            State.CurrentRoundNumberField.Value = 1;
            State.AgeField.Value = 1;
            State.BlockchainStartTimestamp.Value = firstRound.GetStartTime().ToTimestamp();
            State.MiningIntervalField.Value = firstRound.GetMiningInterval();
            State.TermNumberFromMainChainField.Value = firstRound.TermNumber; // init term with main chain term number
        }

        private void UpdateHistoryInformation(Round round)
        {
        }

        private bool TryToGetTermNumber(out long termNumber)
        {
            termNumber = 0;
            return true;
        }

        private Round GenerateFirstRoundOfNextTerm()
        {
            return null;
        }
    }
}