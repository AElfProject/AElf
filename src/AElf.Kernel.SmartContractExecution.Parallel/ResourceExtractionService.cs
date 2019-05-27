using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution.Parallel
{
    public class ResourceExtractionService : IResourceExtractionService
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public ResourceExtractionService(ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task<IEnumerable<TransactionResourceInfo>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions)
        {
            // TODO: Set timeout, maybe assume not parallelizable if timed out
            var tasks = transactions.Select(t => GetResourcesForOneAsync(chainContext, t));
            return await Task.WhenAll(tasks);
        }

        private async Task<TransactionResourceInfo> GetResourcesForOneAsync(IChainContext chainContext,
            Transaction transaction)
        {
            IExecutive executive = null;
            var address = transaction.To;

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