using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class ClaimFeeTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        public ILogger<ClaimFeeTransactionGenerator> Logger { get; set; }

        public ClaimFeeTransactionGenerator(ISmartContractAddressService smartContractAddressService,
            ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
        }

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();

            if (preBlockHeight < AElfConstants.GenesisBlockHeight)
                return generatedTransactions;

            var chainContext = new ChainContext
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            };

            var tokenContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);

            if (tokenContractAddress == null)
                return generatedTransactions;

            var totalTxFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(chainContext);
            if (totalTxFeesMap == null || !totalTxFeesMap.Value.Any() || totalTxFeesMap.BlockHeight != preBlockHeight ||
                totalTxFeesMap.BlockHash != preBlockHash)
            {
                Logger.LogDebug(
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

            Logger.LogTrace("Tx ClaimTransactionFees generated.");
            return generatedTransactions;
        }
    }
}