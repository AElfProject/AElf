using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using  System.Collections.Generic;

namespace AElf.Contracts.TestKit
{
    public interface ITransactionExecutor
    {
        Task ExecuteAsync(Transaction transaction);
        Task<ByteString> ReadAsync(Transaction transaction);

        Task ExecuteAsync(List<Transaction> transactions);
        
        Task<List<ByteString>> ReadAsync(List<Transaction> transaction);
    }
}