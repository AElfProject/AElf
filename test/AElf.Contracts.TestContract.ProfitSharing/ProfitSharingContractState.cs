using System;
using Acs5;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class ProfitSharingContractState : ContractState
    {
        public MappedState<string, MethodProfitFee> MethodProfitFees { get; set; }
    }
}