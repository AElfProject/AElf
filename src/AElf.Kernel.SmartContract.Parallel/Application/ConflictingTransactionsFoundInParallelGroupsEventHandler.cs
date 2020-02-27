using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
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

            var wrongCodeHashList =
                wrongTxWithResources.Select(r => r.TransactionResourceInfo.ContractHash).Distinct();
            
            var dic = wrongCodeHashList.ToDictionary(codeHash => codeHash, codeHash => new NonparallelContractCode
            {
                CodeHash = codeHash,
                NonParallelizable = true
            });
            await _blockchainStateService.AddBlockExecutedDataAsync<Hash, NonparallelContractCode>(
                eventData.BlockHeader.GetHashWithoutCache(), dic);

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds);
        }
    }
}