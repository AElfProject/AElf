using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class ConflictingTransactionsFoundInParallelGroupsEventHandler :
        ILocalEventHandler<ConflictingTransactionsFoundInParallelGroupsEvent>, ITransientDependency
    {
        private readonly IConflictingTransactionIdentificationService _conflictingTransactionIdentificationService;
        private readonly IResourceExtractionService _resourceExtractionService;
        private readonly INonparallelContractCodeProvider _nonparallelContractCodeProvider;
 
        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            IResourceExtractionService resourceExtractionService,
            INonparallelContractCodeProvider nonparallelContractCodeProvider)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _resourceExtractionService = resourceExtractionService;
            _nonparallelContractCodeProvider = nonparallelContractCodeProvider;
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

            await _nonparallelContractCodeProvider.SetNonparallelContractCodeAsync(new BlockIndex
            {
                BlockHash = eventData.BlockHeader.GetHash(),
                BlockHeight = eventData.BlockHeader.Height
            }, dic);

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds);
        }
    }
}