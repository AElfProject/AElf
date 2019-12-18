using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionToAddressValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly IBlockchainService _blockchainService;

        public ILogger<TransactionToAddressValidationProvider> Logger { get; set; }

        public TransactionToAddressValidationProvider(IDeployedContractAddressProvider deployedContractAddressProvider,
            IBlockchainService blockchainService)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _blockchainService = blockchainService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            if (_deployedContractAddressProvider.CheckContractAddress(chainContext, transaction.To))
            {
                return true;
            }

            Logger.LogWarning($"Invalid contract address: {transaction}");
            return false;
        }
    }
}