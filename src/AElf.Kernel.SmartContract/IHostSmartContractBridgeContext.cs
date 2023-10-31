using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract;

public interface IHostSmartContractBridgeContext : ISmartContractBridgeContext
{
    ITransactionContext TransactionContext { get; set; }

    void Initialize(ITransactionContext transactionContext);

    Task<ByteString> GetStateAsync(string key);

    TransactionTrace Execute(Address fromAddress, Address toAddress, string methodName, ByteString args);
}