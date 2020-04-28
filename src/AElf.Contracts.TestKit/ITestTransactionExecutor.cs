using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestKit
{
    public interface ITestTransactionExecutor
    {
        Task<TransactionResult> ExecuteAsync(Transaction transaction);
        Task<TransactionResult> ExecuteWithExceptionAsync(Transaction transaction);
        Task<ByteString> ReadAsync(Transaction transaction);
        Task<StringValue> ReadWithExceptionAsync(Transaction transaction);
    }
}