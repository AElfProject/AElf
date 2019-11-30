using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class BlockAcceptedEventDataHandler : ILocalEventHandler<BlockAcceptedEvent>, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultQueryService _transactionResultQueryService;
        private readonly IContractRemarksService _contractRemarksService;

        public BlockAcceptedEventDataHandler(IBlockchainService blockchainService, 
            ITransactionResultQueryService transactionReadOnlyExecutionService, 
            IContractRemarksService contractRemarksService)
        {
            _blockchainService = blockchainService;
            _transactionResultQueryService = transactionReadOnlyExecutionService;
            _contractRemarksService = contractRemarksService;
        }

        public async Task HandleEventAsync(BlockAcceptedEvent eventData)
        {
            var previousBlockIndex = new BlockIndex
            {
                BlockHash = eventData.Block.Header.PreviousBlockHash, 
                BlockHeight = eventData.Block.Height - 1
            };
            if (!_contractRemarksService.MayHasContractRemarks(previousBlockIndex)) return;
            var transactions = await _blockchainService.GetTransactionsAsync(eventData.Block.TransactionIds);
            var conflictContractAddresses = new List<Address>();
            var blockHash = eventData.Block.GetHash();
            foreach (var transaction in transactions)
            {
                var transactionResult =
                    await _transactionResultQueryService.GetTransactionResultAsync(transaction.GetHash(), blockHash);
                if (transactionResult.Status == TransactionResultStatus.Conflict)
                {
                    conflictContractAddresses.AddIfNotContains(transaction.To);
                }
            }

            foreach (var address in conflictContractAddresses)
            {
                var codeHash = _contractRemarksService.GetCodeHashByBlockIndex(previousBlockIndex, address);
                await _contractRemarksService.SetCodeRemarkAsync(address, codeHash, eventData.Block.Header);
            }
        }
    }
}