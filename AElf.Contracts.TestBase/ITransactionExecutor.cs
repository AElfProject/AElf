using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestBase
{
    public interface ITransactionExecutor
    {
        Task ExecuteAsync(Transaction transaction);
        Task<ByteString> ReadAsync(Transaction transaction);
    }
}