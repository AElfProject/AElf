using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }

        void Initialize(ITransactionContext transactionContext);
        
        Task<ByteString> GetStateAsync(string key);
    }
}