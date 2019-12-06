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
        private readonly IResourceExtractionService _resourceExtractionService;
        private readonly IContractRemarksService _contractRemarksService;
 
        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            IResourceExtractionService resourceExtractionService, 
            IContractRemarksService contractRemarksService)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _resourceExtractionService = resourceExtractionService;
            _contractRemarksService = contractRemarksService;
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

            var wrongAddressAndCodeHashMap = wrongTxWithResources.GroupBy(t => t.Transaction.To)
                .ToDictionary(g => g.Key, g => g.First().TransactionResourceInfo.ContractHash);
            var wrongAddresses = wrongAddressAndCodeHashMap.Keys;
            foreach (var address in wrongAddresses)
            {
                await _contractRemarksService.SetCodeRemarkAsync(address, wrongAddressAndCodeHashMap[address],
                    eventData.BlockHeader);
            }

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds);
        }
    }
}