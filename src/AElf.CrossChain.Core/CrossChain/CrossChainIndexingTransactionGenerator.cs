using System.Collections.Generic;
using System.Linq; 
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    internal class CrossChainIndexingTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;

        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;

        public ILogger<CrossChainIndexingTransactionGenerator> Logger { get; set; }

        public CrossChainIndexingTransactionGenerator(ICrossChainIndexingDataService crossChainIndexingDataService,
            ISmartContractAddressService smartContractAddressService, ITransactionInclusivenessProvider transactionInclusivenessProvider)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _smartContractAddressService = smartContractAddressService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
        }

        private IEnumerable<Transaction> GenerateCrossChainIndexingTransaction(Address from, long refBlockNumber,
            Hash previousBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            var previousBlockPrefix = previousBlockHash.Value.Take(4).ToArray();

            //Logger.LogTrace($"Generate cross chain txn with hash {previousBlockHash}, height {refBlockNumber}");
            
            // should return the same data already filled in block header.
            var filledCrossChainBlockData =
                _crossChainIndexingDataService.GetUsedCrossChainBlockDataForLastMining(previousBlockHash, refBlockNumber);
            
            // filledCrossChainBlockData == null means no cross chain data filled in this block.
            if (filledCrossChainBlockData != null)
            {
                generatedTransactions.Add(GenerateNotSignedTransaction(from, CrossChainConstants.CrossChainIndexingMethodName, refBlockNumber,
                    previousBlockPrefix, filledCrossChainBlockData));
            }
            
            return generatedTransactions;
        }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            if (!_transactionInclusivenessProvider.IsTransactionPackable)
                return;
            generatedTransactions.AddRange(GenerateCrossChainIndexingTransaction(from, preBlockHeight, previousBlockHash));
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="methodName"></param>
        /// <param name="refBlockNumber"></param>
        /// <param name=""></param>
        /// <param name="refBlockPrefix"></param> 
        /// <param name="input"></param>
        /// <returns></returns>
        private Transaction GenerateNotSignedTransaction(Address from, string methodName, long refBlockNumber,
            byte[] refBlockPrefix, IMessage input)
        {
            return new Transaction
            {
                From = from,
                To = _smartContractAddressService.GetAddressByContractName(
                    CrossChainSmartContractAddressNameProvider.Name),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                MethodName = methodName,
                Params = input.ToByteString(),
            };
        }
    }
}