using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// Return true if native token balance of from address is greater than 0.
    /// </summary>
    internal class TransactionFromAddressBalanceValidationForTxHubProvider : ITransactionValidationForTxHubProvider
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITokenContractReaderFactory _tokenContractReaderFactory;
        private readonly INativeTokenSymbolProvider _nativeTokenSymbolProvider;

        public TransactionFromAddressBalanceValidationForTxHubProvider(IBlockchainService blockchainService,
            ITokenContractReaderFactory tokenContractReaderFactory,
            INativeTokenSymbolProvider nativeTokenSymbolProvider)
        {
            _blockchainService = blockchainService;
            _tokenContractReaderFactory = tokenContractReaderFactory;
            _nativeTokenSymbolProvider = nativeTokenSymbolProvider;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var chain = await _blockchainService.GetChainAsync();
            var balance = (await _tokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            }).GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = transaction.From,
                Symbol = _nativeTokenSymbolProvider.GetNativeTokenSymbol()
            })).Balance;
            return balance > 0;
        }
    }
}