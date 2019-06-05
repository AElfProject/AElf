using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel
{
    public interface IResourceExtractionService
    {
        Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct);

        Task HandleTxResourcesNeededAsync(TxResourcesNeededEvent eventData);
        
        Task HandleTxResourcesNoLongerNeededAsync(TxResourcesNoLongerNeededEvent eventData);
    }
    
    public class TxResourcesNeededEvent
    {
        public IEnumerable<Transaction> Transactions { get; set; }
    }
    
    public class TxResourcesNoLongerNeededEvent
    {
        public IEnumerable<Hash> TxIds { get; set; }
    }
}
