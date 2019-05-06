using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
    }
}