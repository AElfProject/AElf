using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroState : ContractState
    {
        public UInt64State ContractSerialNumber { get; set; }
        public MappedState<Address, ContractInfo> ContractInfos { get; set; }
        
        public Int32State ChainId { get; set; }
                
        public MappedState<Hash, SmartContractRegistration> SmartContractRegistrations { get; set; }
        
        public MappedState<Hash, Address> NameAddressMapping { get; set; }
    }
}