using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Blockchain.Application
{
    public interface ITransactionResultQueryService
    {
        Task<TransactionResult> GetTransactionResultAsync(Hash transactionId);
    }

    public interface ITransactionResultService : ITransactionResultQueryService
    {
        Task AddTransactionResultAsync(TransactionResult transactionResult, BlockHeader blockHeader);
    }


    public class TransactionResultService : ITransactionResultService,
        ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
        private readonly ITransactionResultManager _transactionResultManager;

        public TransactionResultService(ITransactionResultManager transactionResultManager,
            ITransactionBlockIndexManager transactionBlockIndexManager, IBlockchainService blockchainService)
        {
            _transactionResultManager = transactionResultManager;
            _transactionBlockIndexManager = transactionBlockIndexManager;
            _blockchainService = blockchainService;
        }

        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            var blockHash = eventData.BlockHash;
            while (true)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);

                var preMiningHash = block.Header.GetPreMiningHash();
                var transactionBlockIndex = new TransactionBlockIndex
                {
                    BlockHash = blockHash
                };
                if (block.Body.Transactions.Count == 0) return;

                var firstTransaction = block.Body.Transactions.First();
                var withBlockHash = await _transactionResultManager.GetTransactionResultAsync(
                    firstTransaction, blockHash);
                var withPreMiningHash = await _transactionResultManager.GetTransactionResultAsync(
                    firstTransaction, preMiningHash);

                if (withBlockHash == null)
                    foreach (var txId in block.Body.Transactions)
                    {
                        var result = await _transactionResultManager.GetTransactionResultAsync(txId, preMiningHash);
                        await _transactionResultManager.AddTransactionResultAsync(result,
                            transactionBlockIndex.BlockHash);
                    }

                if (withPreMiningHash != null)
                    foreach (var txId in block.Body.Transactions)
                        await _transactionResultManager.RemoveTransactionResultAsync(txId, preMiningHash);

                // Add TransactionBlockIndex
                foreach (var txId in block.Body.Transactions)
                    await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(txId, transactionBlockIndex);

                if (block.Height <= eventData.PreviousIrreversibleBlockHeight) break;

                blockHash = block.Header.PreviousBlockHash;
            }
        }

        public async Task AddTransactionResultAsync(TransactionResult transactionResult, BlockHeader blockHeader)
        {
            var disambiguatingHash = blockHeader.IsMined() ? blockHeader.GetHash() : blockHeader.GetPreMiningHash();
            await _transactionResultManager.AddTransactionResultAsync(transactionResult, disambiguatingHash);
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId)
        {
            var transactionBlockIndex =
                await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId);
            if (transactionBlockIndex != null)
                return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    transactionBlockIndex.BlockHash);

            var chain = await _blockchainService.GetChainAsync();
            var hash = chain.BestChainHash;
            var until = chain.LastIrreversibleBlockHeight > KernelConstants.GenesisBlockHeight
                ? chain.LastIrreversibleBlockHeight - 1
                : KernelConstants.GenesisBlockHeight;
            while (true)
            {
                var result = await _transactionResultManager.GetTransactionResultAsync(transactionId, hash);
                if (result != null) return result;

                var header = await _blockchainService.GetBlockHeaderByHashAsync(hash);
                result = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    header.GetPreMiningHash());
                if (result != null) return result;

                if (header.Height <= until) break;

                hash = header.PreviousBlockHash;
            }

            return null;
        }
    }
}