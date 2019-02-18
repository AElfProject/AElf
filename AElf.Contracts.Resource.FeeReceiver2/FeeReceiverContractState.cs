using System;
using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Resource.FeeReceiver
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        public Func<Address, ulong> BalanceOf { get; set; }
        public Action<Address, ulong> Transfer { get; set; }
        public Action<ulong> Burn { get; set; }
    }
    
    public class FeeReceiverContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public TokenContractReferenceState TokenContract { get; set; } 
        public ProtobufState<Address> FoundationAddress { get; set; }
        public UInt64State OwedToFoundation { get; set; }
    }
}