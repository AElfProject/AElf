using System;
using Acs1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.TransactionFeeCharging
{
    public partial class TransactionFeeChargingContractState : ContractState
    {
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
    }
}