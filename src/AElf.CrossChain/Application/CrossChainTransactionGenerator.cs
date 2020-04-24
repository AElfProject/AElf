using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Application;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.CrossChain.Application
{
    internal class CrossChainTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<CrossChainTransactionGenerator> Logger { get; set; }

        public CrossChainTransactionGenerator(ICrossChainIndexingDataService crossChainIndexingDataService,
            ISmartContractAddressService smartContractAddressService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _smartContractAddressService = smartContractAddressService;
        }

        private async Task<List<Transaction>> GenerateCrossChainIndexingTransactionAsync(Address from,
            long refBlockNumber,
            Hash previousBlockHash)
        {
            var generatedTransactions = new List<Transaction>();

            var crossChainTransactionInput =
                await _crossChainIndexingDataService.GetCrossChainTransactionInputForNextMiningAsync(previousBlockHash,
                    refBlockNumber);

            if (crossChainTransactionInput == null)
            {
                return generatedTransactions;
            }

            generatedTransactions.Add(await GenerateNotSignedTransactionAsync(from, crossChainTransactionInput.MethodName, new BlockIndex
            {
                BlockHash = previousBlockHash,
                BlockHeight = refBlockNumber
            }, crossChainTransactionInput.Value));

            Logger.LogTrace($"Cross chain transaction generated.");
            return generatedTransactions;
        }

        public async Task<List<Transaction>> GenerateTransactionsAsync(Address @from, long preBlockHeight,
            Hash preBlockHash)
        {
            var generatedTransactions =
                await GenerateCrossChainIndexingTransactionAsync(from, preBlockHeight, preBlockHash);
            return generatedTransactions;
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="methodName"></param>
        /// <param name="blockIndex"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private async Task<Transaction> GenerateNotSignedTransactionAsync(Address from, string methodName, IBlockIndex blockIndex, ByteString bytes)
        {
            var address = await _smartContractAddressService.GetAddressByContractNameAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                }, CrossChainSmartContractAddressNameProvider.StringName);
            return new Transaction
            {
                From = from,
                To = address,
                RefBlockNumber = blockIndex.BlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(blockIndex.BlockHash.Value.Take(4).ToArray()),
                MethodName = methodName,
                Params = bytes,
            };
        }

        
    }
}