using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Contracts.TestBase2
{
    public interface ITransactionExecutor
    {
        Task ExecuteAsync(Transaction transaction);
        Task<ByteString> ReadAsync(Transaction transaction);
    }
}