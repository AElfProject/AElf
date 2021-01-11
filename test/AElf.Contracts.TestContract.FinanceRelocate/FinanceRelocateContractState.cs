using System;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.FinanceRelocate
{
    public class FinanceRelocateContractState : ContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        public MappedState<string, long> LockedMap { get; set; }
        public MappedState<long, Address> StakeAddressMap { get; set; }
        public Int64State StakeAddressAmount { get; set; }
    }
}