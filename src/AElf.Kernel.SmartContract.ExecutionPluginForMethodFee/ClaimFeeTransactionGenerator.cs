using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class ClaimFeeTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private readonly TransactionPackingOptions _transactionPackingOptions;
        public ILogger<ClaimFeeTransactionGenerator> Logger { get; set; }

        public ClaimFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions,
            ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
        }

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            if (!_transactionPackingOptions.IsTransactionPackable)
                return generatedTransactions;

            if (preBlockHeight < Constants.GenesisBlockHeight)
                return generatedTransactions;

            var tokenContractAddress = _smartContractAddressService.GetAddressByContractName(
                TokenSmartContractAddressNameProvider.Name);

            if (tokenContractAddress == null)
                return generatedTransactions;

            var totalTxFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            });
            if (totalTxFeesMap == null || !totalTxFeesMap.Value.Any() || totalTxFeesMap.BlockHeight != preBlockHeight ||
                totalTxFeesMap.BlockHash != preBlockHash)
            {
                Logger.LogInformation(
                    "Won't generate ClaimTransactionFees because no tx fee charged in previous block.");
                // If previous block doesn't contain logEvent named TransactionFeeCharged, won't generate this tx.
                return new List<Transaction>();
            }

            generatedTransactions.AddRange(new List<Transaction>
            {
                new Transaction
                {
                    From = from,
                    MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ClaimTransactionFees),
                    To = tokenContractAddress,
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash),
                    Params = totalTxFeesMap.ToByteString()
                }
            });

            Logger.LogInformation("Tx ClaimTransactionFees generated.");
            return generatedTransactions;
        }
    }
}