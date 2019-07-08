using Acs0;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroState : ContractState
    {
        public UInt64State ContractSerialNumber { get; set; }
        public MappedState<Address, ContractInfo> ContractInfos { get; set; }

        public Int32State ChainId { get; set; }

        public MappedState<Hash, SmartContractRegistration> SmartContractRegistrations { get; set; }

        public MappedState<Hash, Address> NameAddressMapping { get; set; }
        
        /// <summary>
        /// Genesis owner controls contract deployment if <see cref="ContractDeploymentAuthorityRequired"/> is true.
        /// </summary>
        public SingletonState<Address> GenesisOwner { get; set; }
        
        public SingletonState<bool> ContractDeploymentAuthorityRequired { get; set; } 
        
        public SingletonState<bool> Initialized { get; set; }
    }
}