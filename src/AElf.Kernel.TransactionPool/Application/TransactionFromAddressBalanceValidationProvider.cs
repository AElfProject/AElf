using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
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
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionMethodNameListProvider _coreTransactionMethodNameListProvider;

        public ILogger<TransactionFromAddressBalanceValidationProvider> Logger { get; set; }

        public TransactionFromAddressBalanceValidationProvider(IBlockchainService blockchainService,
            ITokenContractReaderFactory tokenContractReaderFactory,
            IPrimaryTokenSymbolProvider primaryTokenSymbolProvider,
            IDeployedContractAddressProvider deployedContractAddressProvider,
            ISmartContractAddressService smartContractAddressService,
            ISystemTransactionMethodNameListProvider coreTransactionMethodNameListProvider)
        {
            _blockchainService = blockchainService;
            _tokenContractReaderFactory = tokenContractReaderFactory;
            _primaryTokenSymbolProvider = primaryTokenSymbolProvider;
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _smartContractAddressService = smartContractAddressService;
            _coreTransactionMethodNameListProvider = coreTransactionMethodNameListProvider;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            // Skip if the sender is a contract.
            if (_deployedContractAddressProvider.CheckContractAddress(transaction.From))
            {
                return true;
            }

            // Skip if this is a system transaction.
            if (IsSystemTransaction(transaction))
            {
                return true;
            }

            var chain = await _blockchainService.GetChainAsync();

            // Skip this validation at the very beginning of current chain.
            if (chain.LastIrreversibleBlockHeight == Constants.GenesisBlockHeight)
            {
                return true;
            }

            var tokenStub = _tokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });
            var balance = (await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = transaction.From,
                Symbol = await _primaryTokenSymbolProvider.GetPrimaryTokenSymbol()
            }))?.Balance;
            // balance == null means token contract hasn't deployed.
            if (balance == null || balance > 0) return true;

            Logger.LogError($"Empty balance of tx from address: {transaction}");
            return false;
        }

        private bool IsSystemTransaction(Transaction transaction)
        {
            var systemTransactionMethodNameList =
                _coreTransactionMethodNameListProvider.GetSystemTransactionMethodNameList();
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(
                    SmartContractConstants.ConsensusContractSystemName);
            if (transaction.To == consensusContractAddress &&
                systemTransactionMethodNameList.Contains(transaction.MethodName))
            {
                return true;
            }

            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(SmartContractConstants.TokenContractSystemName);
            if (transaction.To == tokenContractAddress &&
                systemTransactionMethodNameList.Contains(transaction.MethodName))
            {
                return true;
            }

            var crossChainContractAddress =
                _smartContractAddressService.GetAddressByContractName(SmartContractConstants
                    .CrossChainContractSystemName);
            if (transaction.To == crossChainContractAddress &&
                systemTransactionMethodNameList.Contains(transaction.MethodName))
            {
                return true;
            }

            return false;
        }
    }
}