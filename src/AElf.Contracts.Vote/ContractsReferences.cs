using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        
        public SingletonState<Hash> TokenContractSystemName { get; set; }
        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
    }
}