using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract
{
    public class SingleTransactionExecutingDto
    {
        public int Depth { get; set; }
        public IChainContext ChainContext { get; set; }
        public Transaction Transaction { get; set; }
        public Timestamp CurrentBlockTime { get; set; }
        public Address Origin { get; set; } = null;    
        public bool IsCancellable { get; set; } = true;
    }
}