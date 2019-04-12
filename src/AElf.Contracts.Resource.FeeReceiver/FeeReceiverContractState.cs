using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Resource.FeeReceiver
{
    public class FeeReceiverContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        public ProtobufState<Address> FoundationAddress { get; set; }
        public Int64State OwedToFoundation { get; set; }
    }
}