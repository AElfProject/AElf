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

        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public ILogger<TransactionToAddressValidationProvider> Logger { get; set; }

        public TransactionToAddressValidationProvider(IBlockchainService blockchainService, 
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _blockchainService = blockchainService;
            _smartContractExecutiveService = smartContractExecutiveService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var chain = await _blockchainService.GetChainAsync();
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            var executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, transaction.To);
            if (executive != null)
            {
                return true;
            }

            Logger.LogWarning($"Invalid contract address: {transaction}");
            return false;
        }
    }
}