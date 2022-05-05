using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract
    {
        private Hash GenerateVoteItemId(VotingRegisterInput input)
        {
            var votingItemId = input.GetHash(Context.Sender);
            return votingItemId;
        }
        private void MakeSureReferenceStateAddressSet(ContractReferenceState state, string contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }
    }
}