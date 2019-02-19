using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Genesis
{
    public class AuthorizationContractReferenceState : ContractReferenceState
    {
        public Action<Proposal> Propose { get; set; }
        public Func<Address, bool> IsMultiSigAccount { get; set; }
        public Func<Address, Authorization> GetAuthorization { get; set; }
    }
    
    public class BasicContractZeroState : ContractState
    {
        public BoolState Initialized { get; set; }
        public AuthorizationContractReferenceState AuthorizationContract { get; set; }
        public UInt64State ContractSerialNumber { get; set; }
        public MappedState<Address, ContractInfo> ContractInfos { get; set; }
    }
}