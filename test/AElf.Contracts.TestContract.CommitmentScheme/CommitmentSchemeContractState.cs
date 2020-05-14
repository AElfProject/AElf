﻿using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.CommitmentScheme
{
    public partial class CommitmentSchemeContractState : ContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public MappedState<Address, Hash> Commitments { get; set; }
        public MappedState<Address, RequestSlot> RequestSlots { get; set; }
    }
}