using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState
    {
        internal VoteContractContainer.VoteContractReferenceState VoteContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
        
        public SingletonState<Hash> VoteContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
    }
}