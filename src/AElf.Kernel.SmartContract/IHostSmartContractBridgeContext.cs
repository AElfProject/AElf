using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract
{
    public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
    {
        ITransactionContext TransactionContext { get; set; }

        Address GetContractAddressByName(Hash hash);

        void Initialize(ITransactionContext transactionContext);
        
        Task<ByteString> GetStateAsync(string key);
    }
}