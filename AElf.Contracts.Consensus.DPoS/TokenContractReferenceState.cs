using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        internal Action<LockInput> Lock { get; set; }
        internal Action<UnlockInput> Unlock { get; set; }
    }
}