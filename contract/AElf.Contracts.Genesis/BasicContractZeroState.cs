using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Standards.ACS1;
using AElf.Types;

namespace AElf.Contracts.Genesis;

public partial class BasicContractZeroState : ContractState
{
    public Int64State ContractSerialNumber { get; set; }
    public MappedState<Address, ContractInfo> ContractInfos { get; set; }

    public MappedState<Hash, SmartContractRegistration> SmartContractRegistrations { get; set; }

    public MappedState<Hash, Address> NameAddressMapping { get; set; }

    public MappedState<Hash, ContractProposingInput> ContractProposingInputMap { get; set; }

    /// <summary>
    ///     Genesis owner controls contract deployment if <see cref="ContractDeploymentAuthorityRequired" /> is true.
    /// </summary>
    public SingletonState<AuthorityInfo> ContractDeploymentController { get; set; }

    public SingletonState<AuthorityInfo> CodeCheckController { get; set; }

    public SingletonState<bool> ContractDeploymentAuthorityRequired { get; set; }

    public SingletonState<bool> Initialized { get; set; }

    public MappedState<string, MethodFees> TransactionFees { get; set; }

    public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

    public MappedState<long, ContractCodeHashList> ContractCodeHashListMap { get; set; }

    public SingletonState<int> ContractProposalExpirationTimePeriod { get; set; }
    
    public MappedState<Address, Address> SignerMap { get; set; }

    public SingletonState<int> CodeCheckProposalExpirationTimePeriod { get; set; }

}