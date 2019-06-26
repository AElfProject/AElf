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

        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            ICodeRemarksService codeRemarksService)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _codeRemarksService = codeRemarksService;
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
            foreach (var transaction in wrong)
            {
                await _codeRemarksService.MarkUnparallelizableAsync(chainContext, transaction.To);
            }
        }
    }
}