using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public class ResourceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<StringValue, Converter> Converters { get; set; }
        public MappedState<UserResourceKey, long> UserBalances { get; set; }
        public MappedState<UserResourceKey, long> LockedUserResources { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        public ProtobufState<Address> FeeAddress { get; set; }
        public ProtobufState<Address> ResourceControllerAddress { get; set; }
    }
}