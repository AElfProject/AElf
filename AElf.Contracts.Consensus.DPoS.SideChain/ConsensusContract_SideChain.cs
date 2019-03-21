using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public override Empty UpdateMainChainConsensus(DPoSInformation input)
        {
            // TODO: Only cross chain contract can call UpdateMainChainConsensus method of consensus contract.
            
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            if(input == null || input == new DPoSInformation())
                return new Empty();
            var consensusInformation = input;
            if(consensusInformation.Round.TermNumber <= State.TermNumberFromMainChainField.Value)
                return new Empty();
            Context.LogDebug(() => $"Shared BP of term {consensusInformation.Round.TermNumber.ToInt64Value()}");
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.TermNumberFromMainChainField.Value = consensusInformation.Round.TermNumber;
            State.CurrentMiners.Value = minersKeys.ToList().ToMiners();
            return new Empty();
        }
        
        private bool GenerateNextRoundInformation(Round currentRound, Timestamp timestamp,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            if (State.CurrentMiners.Value == null || currentRound.RealTimeMinersInformation.Keys.ToList().ToMiners().GetMinersHash() ==
                State.CurrentMiners.Value.GetMinersHash())
            {
                return currentRound.GenerateNextRoundInformation(timestamp, blockchainStartTimestamp, out nextRound);
            }

            nextRound = State.CurrentMiners.Value.GenerateFirstRoundOfNewTerm(currentRound.GetMiningInterval(),
                currentRound.RoundNumber, State.TermNumberFromMainChainField.Value);
            return true;
        }

        private void InitialSettings(Round firstRound)
        {
            // Do some initializations.
            State.CurrentRoundNumberField.Value = 1;
            State.AgeField.Value = 1;
            State.BlockchainStartTimestamp.Value = firstRound.GetStartTime();
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