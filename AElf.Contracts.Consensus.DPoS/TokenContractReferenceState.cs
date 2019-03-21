using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        internal MethodReference<LockInput, Empty> Lock { get; set; }
        internal MethodReference<UnlockInput, Empty> Unlock { get; set; }
    }
}