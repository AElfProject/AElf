using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("AElf.WebApp.Application.TestBase")]

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// Return true if native token balance of from address is greater than 0.
    /// </summary>
    internal class TransactionFromAddressBalanceValidationProvider : ITransactionValidationProvider
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITokenContractReaderFactory _tokenContractReaderFactory;
        private readonly IPrimaryTokenSymbolProvider _primaryTokenSymbolProvider;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly ITransactionFeeExemptionService _feeExemptionService;

        public ILogger<TransactionFromAddressBalanceValidationProvider> Logger { get; set; }

        public TransactionFromAddressBalanceValidationProvider(IBlockchainService blockchainService,
            ITokenContractReaderFactory tokenContractReaderFactory,
            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
            IDeployedContractAddressProvider deployedContractAddressProvider,
            ITransactionFeeExemptionService feeExemptionService)
        {
            _blockchainService = blockchainService;
            _tokenContractReaderFactory = tokenContractReaderFactory;
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _feeExemptionService = feeExemptionService;
        }

        public bool ValidateWhileSyncing => false;

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            // Skip if this is a system transaction.
            if (_feeExemptionService.IsFree(transaction))
            {
                return true;
            }

            var chain = await _blockchainService.GetChainAsync();

            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };

            // Skip if the sender is a contract.
            if (_deployedContractAddressProvider.CheckContractAddress(chainContext, transaction.From))
            {
                return true;
            }

            // Skip this validation at the very beginning of current chain.
            if (chain.LastIrreversibleBlockHeight == Constants.GenesisBlockHeight)
            {
                return true;
            }

            var tokenStub = _tokenContractReaderFactory.Create(chainContext);
            var balance = (await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = transaction.From,
                Symbol = await _primaryTokenSymbolProvider.GetPrimaryTokenSymbol()
            }))?.Balance;
            // balance == null means token contract hasn't deployed.
            if (balance == null || balance > 0) return true;

            Logger.LogWarning($"Empty balance of tx from address: {transaction}");
            return false;
        }
    }
}