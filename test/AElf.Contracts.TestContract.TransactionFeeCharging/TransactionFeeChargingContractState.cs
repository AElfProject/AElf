using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Contracts.TestContract.TransactionFeeCharging;

public partial class TransactionFeeChargingContractState : ContractState
{
    public MappedState<string, MethodFees> TransactionFees { get; set; }
}