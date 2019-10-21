using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ConflictingTransactionsFoundInParallelGroupsEventHandler :
        ILocalEventHandler<ConflictingTransactionsFoundInParallelGroupsEvent>, ITransientDependency
    {
        private readonly IConflictingTransactionIdentificationService _conflictingTransactionIdentificationService;
        private readonly ICodeRemarksService _codeRemarksService;
        private readonly IResourceExtractionService _resourceExtractionService;
 
        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            ICodeRemarksService codeRemarksService,IResourceExtractionService resourceExtractionService)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _codeRemarksService = codeRemarksService;
            _resourceExtractionService = resourceExtractionService;
        }

        public async Task HandleEventAsync(ConflictingTransactionsFoundInParallelGroupsEvent eventData)
        {
            var chainContext = new ChainContext
            {
                BlockHash = eventData.PreviousBlockHash,
                BlockHeight = eventData.PreviousBlockHeight
            };
            var wrong = await _conflictingTransactionIdentificationService.IdentifyConflictingTransactionsAsync(
                chainContext, eventData.ExistingSets, eventData.ConflictingSets);
            
            var wrongTransactionIds = wrong.Select(t => t.GetHash()).ToArray();
            eventData.ConflictingSets.RemoveAll(t => !t.TransactionId.IsIn(wrongTransactionIds));

            var wrongTransactionAddresses = wrong.Select(t => t.To).Distinct().ToList();
            foreach (var address in wrongTransactionAddresses)
            {
                await _codeRemarksService.MarkUnparallelizableAsync(chainContext, address);
            }

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds,
                wrongTransactionAddresses);
        }
    }
}