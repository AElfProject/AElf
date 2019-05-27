using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Vote
{
    public partial class VoteContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}