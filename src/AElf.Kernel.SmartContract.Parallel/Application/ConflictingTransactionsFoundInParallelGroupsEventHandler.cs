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
        private readonly IContractRemarksManager _contractRemarksManager;
 
        public ConflictingTransactionsFoundInParallelGroupsEventHandler(
            IConflictingTransactionIdentificationService conflictingTransactionIdentificationService,
            IResourceExtractionService resourceExtractionService, 
            IContractRemarksManager contractRemarksManager)
        {
            _conflictingTransactionIdentificationService = conflictingTransactionIdentificationService;
            _resourceExtractionService = resourceExtractionService;
            _contractRemarksManager = contractRemarksManager;
        }

        public async Task HandleEventAsync(ConflictingTransactionsFoundInParallelGroupsEvent eventData)
        {
            var chainContext = new ChainContext
            {
                BlockHash = eventData.PreviousBlockHash,
                BlockHeight = eventData.PreviousBlockHeight
            };
            var wrongTxWithResources = await _conflictingTransactionIdentificationService.IdentifyConflictingTransactionsAsync(
                chainContext, eventData.ExistingSets, eventData.ConflictingSets);
            
            var wrongTransactionIds = wrongTxWithResources.Select(t => t.Transaction.GetHash()).ToArray();
            eventData.ConflictingSets.RemoveAll(t => !t.TransactionId.IsIn(wrongTransactionIds));

            var wrongAddressAndCodeHashMap = wrongTxWithResources.GroupBy(t => t.Transaction.To)
                .ToDictionary(g => g.Key, g => g.First().TransactionResourceInfo.ContractHash);
            var wrongAddresses = wrongAddressAndCodeHashMap.Keys;
            foreach (var address in wrongAddresses)
            {
                _contractRemarksManager.AddCodeHashCache(
                    new BlockIndex
                        {BlockHash = eventData.PreviousBlockHash, BlockHeight = eventData.PreviousBlockHeight},
                    address, wrongAddressAndCodeHashMap[address]);
            }

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds);
        }
    }
}