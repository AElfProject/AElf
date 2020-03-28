using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{

    public class ExecutedBlock
    {
        public Block Block { get; set; }
        public IDictionary<Hash,TransactionResult> TransactionResultMap { get; set; }
        
        public IDictionary<Hash,Transaction> TransactionMap { get; set; }
    }
    
    public interface IBlockExecutingService
    {
        Task<ExecutedBlock> ExecuteBlockAsync(BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions);

        Task<ExecutedBlock> ExecuteBlockAsync(BlockHeader blockHeader, IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken);
    }
}