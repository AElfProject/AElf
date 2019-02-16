using System;
using AElf.Common;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource
{
    public class UserBalanceMappedState : MappedState<UserResourceKey, ulong>
    {
    }
    
    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Transfer { get; set; }
        public Action<Address, Address, ulong> TransferFrom { get; set; }
    }
    
    public class ResourceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<StringValue, Converter> Converters { get; set; }
        public UserBalanceMappedState UserBalances { get; set; }
        public UserBalanceMappedState LockedUserResources { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
        public ProtobufState<Address> FeeAddress { get; set; }
        public ProtobufState<Address> ResourceControllerAddress { get; set; }
    }
}