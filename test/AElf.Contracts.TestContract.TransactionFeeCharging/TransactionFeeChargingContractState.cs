using System;
using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.TransactionFeeCharging
{
    public partial class TransactionFeeChargingContractState : ContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }
    }
}