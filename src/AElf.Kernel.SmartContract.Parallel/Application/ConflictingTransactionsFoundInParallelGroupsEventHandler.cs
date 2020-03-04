using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class ConflictingTransactionsFoundInParallelGroupsEventHandler :
        ILocalEventHandler<ConflictingTransactionsFoundInParallelGroupsEvent>, ITransientDependency
    {
        private readonly IConflictingTransactionIdentificationService _conflictingTransactionIdentificationService;
        private readonly IResourceExtractionService _resourceExtractionService;
        private readonly IBlockchainStateService _blockchainStateService;
 
        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            IResourceExtractionService resourceExtractionService, 
            IBlockchainStateService blockchainStateService)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _resourceExtractionService = resourceExtractionService;
            _blockchainStateService = blockchainStateService;
        }

        public async Task HandleEventAsync(ConflictingTransactionsFoundInParallelGroupsEvent eventData)
        {
            var chainContext = new ChainContext
            {
                BlockHash = eventData.BlockHeader.PreviousBlockHash,
                BlockHeight = eventData.BlockHeader.Height - 1
            };
            var wrongTxWithResources = await _conflictingTransactionIdentificationService.IdentifyConflictingTransactionsAsync(
                chainContext, eventData.ExistingSets, eventData.ConflictingSets);
            
            var wrongTransactionIds = wrongTxWithResources.Select(t => t.Transaction.GetHash()).ToArray();

            var dic = wrongTxWithResources.GroupBy(t => t.Transaction.To)
                .ToDictionary(g => g.Key, g => new NonparallelContractCode
                {
                    CodeHash = g.First().TransactionResourceInfo.ContractHash
                });

            await _blockchainStateService.AddBlockExecutedDataAsync<Address, NonparallelContractCode>(
                eventData.BlockHeader.GetHash(), dic);

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds);
        }
    }
}