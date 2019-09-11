using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Blockchain.Application
{
    public interface ITransactionResultQueryService
    {
        Task<TransactionResult> GetTransactionResultAsync(Hash transactionId);
        //Task<TransactionResult> GetTransactionResultAsync2(Hash transactionId);
        Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash);
    }

    public interface ITransactionResultService : ITransactionResultQueryService
    {
        Task AddTransactionResultAsync(TransactionResult transactionResult, BlockHeader blockHeader);
    }


    public class TransactionResultService : ITransactionResultService, ITransientDependency
    {
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
        private readonly IBlockchainService _blockchainService;

        public TransactionResultService(ITransactionResultManager transactionResultManager,
            ITransactionBlockIndexManager transactionBlockIndexManager, IBlockchainService blockchainService)
        {
            _transactionResultManager = transactionResultManager;
            _transactionBlockIndexManager = transactionBlockIndexManager;
            _blockchainService = blockchainService;
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

            var chain = await _blockchainService.GetChainAsync();
            if (transactionBlockIndex != null)
            {
                var blockHashInBestChain =
                    await _blockchainService.GetBlockHashByHeightAsync(chain, transactionBlockIndex.BlockHeight,
                        chain.BestChainHash);
                
                // check whether it is on best chain or a fork branch
                if (transactionBlockIndex.BlockHash == blockHashInBestChain)
                    // If TransactionBlockIndex exists, then read the result via TransactionBlockIndex
                    return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                        transactionBlockIndex.BlockHash);
            }

            var hash = chain.BestChainHash;
            var until = chain.LastIrreversibleBlockHeight > Constants.GenesisBlockHeight
                ? chain.LastIrreversibleBlockHeight - 1
                : Constants.GenesisBlockHeight;
            while (true)
            {
                var result = await _transactionResultManager.GetTransactionResultAsync(transactionId, hash);
                if (result != null)
                {
                    return result;
                }

                var header = await _blockchainService.GetBlockHeaderByHashAsync(hash);
                result = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    header.GetPreMiningHash());
                if (result != null)
                {
                    return result;
                }

                if (header.Height <= until)
                {
                    // do until 1 block below LIB, in case the TransactionBlockIndex is not already added during
                    // NewIrreversibleBlockFoundEvent handling
                    break;
                }

                hash = header.PreviousBlockHash;
            }

            return null;
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash)
        {
            var txResult = await _transactionResultManager.GetTransactionResultAsync(transactionId, blockHash);
            if (txResult != null)
            {
                return txResult;
            }
            var header = await _blockchainService.GetBlockHeaderByHashAsync(blockHash);
            txResult = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                header.GetPreMiningHash());
            
            return txResult;
        }

        private async Task<TransactionResult> GetTransactionResultAsync2(Hash transactionId)
        {
            var transactionBlockIndex =
                await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId);
            if (transactionBlockIndex != null)
            {
                // If TransactionBlockIndex exists, then read the result via TransactionBlockIndex
                return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    transactionBlockIndex.BlockHash);
            }
            var chain = await _blockchainService.GetChainAsync();
            var hash = chain.BestChainHash;
            var until = chain.LastIrreversibleBlockHeight > Constants.GenesisBlockHeight
                ? chain.LastIrreversibleBlockHeight - 1
                : Constants.GenesisBlockHeight;
            while (true)
            {
                var blockInfo = await _blockchainService.GetBlockByHashAsync(hash);
                var transactionIds = blockInfo.Body.TransactionIds;
                if (transactionIds.Contains(transactionId))
                {
                    var res = await _transactionResultManager.GetTransactionResultAsync(transactionId, hash);
                    if (res != null)
                    {
                        return res;
                    }
                    res = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                        blockInfo.Header.GetPreMiningHash());

                    return res;
                }
                
                if (blockInfo.Header.Height <= until)
                {
                    // do until 1 block below LIB, in case the TransactionBlockIndex is not already added during
                    // NewIrreversibleBlockFoundEvent handling
                    break;
                }

                hash = blockInfo.Header.PreviousBlockHash;
            }

            return null;
        }
    }
}