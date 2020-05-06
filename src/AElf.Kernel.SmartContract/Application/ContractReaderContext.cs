using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public class ContractReaderContext
    {
        public Address Sender { get; set; }
        public Address ContractAddress { get; set; }
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }
        public Timestamp Timestamp { get; set; }
        public IStateCache StateCache { get; set; }
    }
}