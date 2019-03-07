using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class ConsensusContract : ISideChainDPoSConsensusSmartContract
    {
        public void UpdateMainChainConsensus(byte[] consensusInformationBytes)
        {
            // TODO: Only cross chain contract can call UpdateMainChainConsensus method of consensus contract.
            
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            var consensusInformation = DPoSInformation.Parser.ParseFrom(consensusInformationBytes);
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.CurrentMiners.Value = minersKeys.ToMiners(1);
        }
        
        private bool GenerateNextRoundInformation(Round currentRound, Timestamp timestamp,
            Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            if (currentRound.RealTimeMinersInformation.Keys.ToMiners().GetMinersHash() == State.CurrentMiners.Value.GetMinersHash())
            {
                return currentRound.GenerateNextRoundInformation(timestamp, blockchainStartTimestamp, out nextRound);
            }

            nextRound = State.CurrentMiners.Value.GenerateFirstRoundOfNewTerm(currentRound.GetMiningInterval());
            return true;
        }

        private void InitialSettings(Round firstRound)
        {
            // Do some initializations.
            State.CurrentRoundNumberField.Value = 1;
            State.AgeField.Value = 1;
            State.BlockchainStartTimestamp.Value = firstRound.GetStartTime();
            State.MiningIntervalField.Value = firstRound.GetMiningInterval();
        }

        private void UpdateHistoryInformation(Round round)
        {
        }

        private bool TryToGetTermNumber(out ulong termNumber)
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