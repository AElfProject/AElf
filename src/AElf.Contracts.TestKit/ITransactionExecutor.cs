using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.TestKit
{
    public interface ITransactionExecutor
    {
        Task ExecuteAsync(Transaction transaction);
        Task<ByteString> ReadAsync(Transaction transaction);
    }
}