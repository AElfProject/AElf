using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;
using AElf.Types;

namespace AElf.Contracts.Profit;

public partial class ProfitContractState : ContractState
{
    public MappedState<Hash, Scheme> SchemeInfos { get; set; }

    public MappedState<Address, DistributedProfitsInfo> DistributedProfitsMap { get; set; }

    public MappedState<Hash, Address, ProfitDetails> ProfitDetailsMap { get; set; }

    public MappedState<Address, CreatedSchemeIds> ManagingSchemeIds { get; set; }

    public MappedState<string, MethodFees> TransactionFees { get; set; }

    public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    
    public BoolState BackupSubsidyTotalShareIncreased { get; set; }
}