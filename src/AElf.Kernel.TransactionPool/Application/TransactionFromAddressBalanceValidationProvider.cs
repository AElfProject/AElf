using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// Return true if native token balance of from address is greater than 0.
    /// </summary>
    internal class TransactionFromAddressBalanceValidationProvider : ITransactionValidationProvider
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITokenContractReaderFactory _tokenContractReaderFactory;
        private readonly INativeTokenSymbolProvider _nativeTokenSymbolProvider;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionMethodNameListProvider _coreTransactionMethodNameListProvider;

        public ILogger<TransactionFromAddressBalanceValidationProvider> Logger { get; set; }

        public TransactionFromAddressBalanceValidationProvider(IBlockchainService blockchainService,
            ITokenContractReaderFactory tokenContractReaderFactory,
            INativeTokenSymbolProvider nativeTokenSymbolProvider,
            IDeployedContractAddressProvider deployedContractAddressProvider,
            ISmartContractAddressService smartContractAddressService,
            ISystemTransactionMethodNameListProvider coreTransactionMethodNameListProvider)
        {
            _blockchainService = blockchainService;
            _tokenContractReaderFactory = tokenContractReaderFactory;
            _nativeTokenSymbolProvider = nativeTokenSymbolProvider;
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _smartContractAddressService = smartContractAddressService;
            _coreTransactionMethodNameListProvider = coreTransactionMethodNameListProvider;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            // Skip if the sender is a contract.
            var deployedContractAddressList =
                await _deployedContractAddressProvider.GetDeployedContractAddressListAsync();
            if (deployedContractAddressList.Value.Contains(transaction.From))
            {
                return true;
            }

            // Skip if this is a consensus transaction.
            var systemTransactionMethodNameList =
                _coreTransactionMethodNameListProvider.GetSystemTransactionMethodNameList();
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            if (transaction.To == consensusContractAddress &&
                systemTransactionMethodNameList.Contains(transaction.MethodName))
            {
                return true;
            }

            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (transaction.To == tokenContractAddress &&
                systemTransactionMethodNameList.Contains(transaction.MethodName))
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
                Symbol = _nativeTokenSymbolProvider.GetNativeTokenSymbol()
            }))?.Balance;
            // balance == null means token contract hasn't deployed.
            if (balance == null || balance > 0) return true;

            Logger.LogError($"Empty balance of tx from address: {transaction}");
            return false;
        }
    }
}