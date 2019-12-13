using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract
{
    public class ExecutePluginTransactionDto
    {
        public IExecutive Executive { get; set; }
        public ITransactionContext TxContext { get; set; }
        public Timestamp CurrentBlockTime { get; set; }
        public IChainContext InternalChainContext { get; set; }
        public TieredStateCache InternalStateCache { get; set; }
    }
}