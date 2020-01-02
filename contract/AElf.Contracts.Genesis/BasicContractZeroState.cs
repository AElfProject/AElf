using Acs0;
using Acs1;
using Acs3;
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
        
        public MappedState<Hash, ContractProposingInput> ContractProposingInputMap { get; set; }
        
        /// <summary>
        /// Genesis owner controls contract deployment if <see cref="ContractDeploymentAuthorityRequired"/> is true.
        /// </summary>
        public SingletonState<AuthorityStuff> ContractDeploymentController { get; set; }
        
        public SingletonState<AuthorityStuff> CodeCheckController { get; set; }
        
        public SingletonState<bool> ContractDeploymentAuthorityRequired { get; set; }
        
        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<AddressList> DeployedContractAddressList { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
    }
}