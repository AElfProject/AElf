﻿using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState : ContractState
    {
        public SingletonState<Address> ConfigurationController { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
        public MappedState<string, BytesValue> Configurations { get; set; }
    }
}