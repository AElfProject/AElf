using System;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class BasicContractZeroReferenceState : ContractReferenceState
    {
        internal MethodReference<Hash, Address> GetContractAddressByName { get; set; }
    }
}