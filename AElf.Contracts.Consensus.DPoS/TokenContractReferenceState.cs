using System;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<string, string, ulong, uint> Initialize { get; set; }
        public Action<Address, ulong> Lock { get; set; }
        public Action<Address, ulong> Unlock { get; set; }
    }
}