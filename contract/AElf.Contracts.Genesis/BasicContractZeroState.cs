using Acs0;
using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZeroState : ContractState
    {
        public UInt64State ContractSerialNumber { get; set; }
        public MappedState<Address, ContractInfo> ContractInfos { get; set; }

        public MappedState<Hash, SmartContractRegistration> SmartContractRegistrations { get; set; }

        public MappedState<Hash, Address> NameAddressMapping { get; set; }
        
        /// <summary>
        /// Genesis owner controls contract deployment if <see cref="ContractDeploymentAuthorityRequired"/> is true.
        /// </summary>
        public SingletonState<Address> GenesisOwner { get; set; }
        
        public SingletonState<bool> ContractDeploymentAuthorityRequired { get; set; } 
        
        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<AddressList> DeployedContractAddressList { get; set; }
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
    }
}