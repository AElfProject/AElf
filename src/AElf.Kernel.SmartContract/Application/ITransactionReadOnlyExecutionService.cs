using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ITransactionReadOnlyExecutionService
    {
        Task<TransactionTrace> ExecuteAsync(IChainContext chainContext, Transaction transaction,
            Timestamp currentBlockTime);
        
        Task<List<TransactionTrace>> ExecuteAsync(IChainContext chainContext, IList<Transaction> transaction,
            Timestamp currentBlockTime);

        Task<byte[]> GetFileDescriptorSetAsync(IChainContext chainContext, Address address);

        Task<IEnumerable<FileDescriptor>> GetFileDescriptorsAsync(IChainContext chainContext, Address address);

        Task<string> GetTransactionParametersAsync(IChainContext chainContext, Transaction transaction);
    }
}