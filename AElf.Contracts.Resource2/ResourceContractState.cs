using System;
using AElf.Common;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Transfer { get; set; }
        public Action<Address, Address, ulong> TransferFrom { get; set; }
    }
    
    public class ResourceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<StringValue, Converter> Converters { get; set; }
        public MappedState<UserResourceKey, ulong> UserBalances { get; set; }
        public MappedState<UserResourceKey, ulong> LockedUserResources { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
        public ProtobufState<Address> FeeAddress { get; set; }
        public ProtobufState<Address> ResourceControllerAddress { get; set; }
    }
}