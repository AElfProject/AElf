using AElf.Contracts.Consensus.AElfConsensus;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState
    {
        internal VoteContractContainer.VoteContractReferenceState VoteContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        internal AElfConsensusContractContainer.AElfConsensusContractReferenceState AElfConsensusContract { get; set; }

        public SingletonState<Hash> VoteContractSystemName { get; set; }
        public SingletonState<Hash> ProfitContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        public SingletonState<Hash> AElfConsensusContractSystemName { get; set; }
    }
}