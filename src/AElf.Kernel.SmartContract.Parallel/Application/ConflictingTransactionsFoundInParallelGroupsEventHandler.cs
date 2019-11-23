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
            var wrong = await _conflictingTransactionIdentificationService.IdentifyConflictingTransactionsAsync(
                chainContext, eventData.ExistingSets, eventData.ConflictingSets);
            
            var wrongTransactionIds = wrong.Select(t => t.Item1.GetHash()).ToArray();
            eventData.ConflictingSets.RemoveAll(t => !t.TransactionId.IsIn(wrongTransactionIds));

            var wrongAddressAndCodeHashMap = wrong.GroupBy(t => t.Item1.To)
                .ToDictionary(g => g.Key, g => g.First().Item2.ContractHash);
            var wrongAddresses = wrongAddressAndCodeHashMap.Keys;
            foreach (var address in wrongAddresses)
            {
                _contractRemarksManager.AddCodeHashCache(
                    new BlockIndex
                        {BlockHash = eventData.PreviousBlockHash, BlockHeight = eventData.PreviousBlockHeight},
                    address, wrongAddressAndCodeHashMap[address]);
            }

            _resourceExtractionService.ClearConflictingTransactionsResourceCache(wrongTransactionIds,
                wrongAddresses);
        }
    }
}