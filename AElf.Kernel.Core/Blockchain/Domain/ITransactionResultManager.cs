using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface ITransactionResultManager
    {
        Task AddTransactionResultAsync(TransactionResult transactionResult, Hash disambiguationHash);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash);
    }

    public class TransactionResultManager : ITransactionResultManager
    {
        private readonly IBlockchainStore<TransactionResult> _transactionResultStore;

        public TransactionResultManager(IBlockchainStore<TransactionResult> transactionResultStore)
        {
            _transactionResultStore = transactionResultStore;
        }

        public async Task AddTransactionResultAsync(TransactionResult transactionResult, Hash disambiguationHash)
        {
            await _transactionResultStore.SetAsync(transactionResult.TransactionId.Xor(disambiguationHash).ToHex(),
                transactionResult);
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId, Hash disambiguationHash)
        {
            return await _transactionResultStore.GetAsync(txId.Xor(disambiguationHash).ToHex());
        }
    }
}