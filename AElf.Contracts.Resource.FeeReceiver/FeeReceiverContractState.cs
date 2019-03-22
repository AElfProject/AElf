using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Resource.FeeReceiver
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        internal MethodReference<GetBalanceInput, GetBalanceOutput> GetBalance { get; set; }
        internal MethodReference<TransferInput, Empty> Transfer { get; set; }
        internal MethodReference<BurnInput, Empty> Burn { get; set; }
    }
    
    public class FeeReceiverContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public TokenContractReferenceState TokenContract { get; set; } 
        public ProtobufState<Address> FoundationAddress { get; set; }
        public Int64State OwedToFoundation { get; set; }
    }
}