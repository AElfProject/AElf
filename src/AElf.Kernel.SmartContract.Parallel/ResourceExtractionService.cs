using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ResourceExtractionService : IResourceExtractionService, ISingletonDependency
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public ResourceExtractionService(ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct)
        {
            var tasks = transactions.Select(t => GetResourcesForOneAsync(chainContext, t, ct));
            return await Task.WhenAll(tasks);
        }

        private async Task<(Transaction, TransactionResourceInfo)> GetResourcesForOneAsync(IChainContext chainContext,
            Transaction transaction, CancellationToken ct)
        {
            IExecutive executive = null;
            var address = transaction.To;

            if (ct.IsCancellationRequested)
                return (transaction, new TransactionResourceInfo()
                {
                    TransactionId = transaction.GetHash(),
                    NonParallelizable = true
                });
            
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
                return await executive.GetTransactionResourceInfoAsync(chainContext, transaction);
            }
            finally
            {
                if (executive != null)
                {
                    await _smartContractExecutiveService.PutExecutiveAsync(address, executive);
                }
            }
        }
    }
}