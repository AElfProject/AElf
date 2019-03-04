using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application
{
    public interface ITransactionResultSettingService
    {
        Task AddTransactionResultAsync(TransactionResult transactionResult, BlockHeader blockHeader);
    }

    public interface ITransactionResultGettingService
    {
        Task<TransactionResult> GetTransactionResultAsync(Hash transactionId);
    }

    public interface ITransactionBlockIndexSettingService
    {
        Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex);
    }

    public class TransactionResultService : ITransactionResultSettingService, ITransactionResultGettingService,
        ITransactionBlockIndexSettingService
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
            if (transactionBlockIndex != null)
            {
                return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    transactionBlockIndex.BlockHash);
            }

            var chain = await _blockchainService.GetChainAsync();
            var hash = chain.BestChainHash;
            while (hash != chain.LastIrreversibleBlockHash)
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

                hash = header.PreviousBlockHash;
            }

            return null;
        }

        public async Task SetTransactionBlockIndexAsync(Hash transactionId, TransactionBlockIndex transactionBlockIndex)
        {
            await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(transactionId, transactionBlockIndex);
        }
    }
}