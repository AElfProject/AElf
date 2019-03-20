using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        internal MethodReference<LockInput, MultiToken.Messages.Nothing> Lock { get; set; }
        internal MethodReference<UnlockInput, MultiToken.Messages.Nothing> Unlock { get; set; }
    }
}