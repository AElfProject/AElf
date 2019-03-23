using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.CrossChain
{
    public class CrossChainIndexingTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainService _crossChainService;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<CrossChainIndexingTransactionGenerator> Logger { get; set; }

        public CrossChainIndexingTransactionGenerator(ICrossChainService crossChainService,
            ISmartContractAddressService smartContractAddressService)
        {
            _crossChainService = crossChainService;
            _smartContractAddressService = smartContractAddressService;
        }

        private IEnumerable<Transaction> GenerateCrossChainIndexingTransaction(Address from, long refBlockNumber,
            Hash previousBlockHash)
        {
//            var sideChainBlockData = await _crossChainService.GetSideChainBlockDataAsync(previousBlockHash, refBlockNumber);
//            var parentChainBlockData = await _crossChainService.GetParentChainBlockDataAsync(previousBlockHash, refBlockNumber);
//            if (parentChainBlockData.Count == 0 && sideChainBlockData.Count == 0)
//                return generatedTransactions;
//            
//            var crossChainBlockData = new CrossChainBlockData();
//            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
//            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            
            var generatedTransactions = new List<Transaction>();
            var previousBlockPrefix = previousBlockHash.Value.Take(4).ToArray();

            //Logger.LogTrace($"Generate cross chain txn with hash {previousBlockHash}, height {refBlockNumber}");
            
            // should return the same data already filled in block header.
            var filledCrossChainBlockData =
                _crossChainService.GetCrossChainBlockDataFilledInBlock(previousBlockHash, refBlockNumber);
            
            // filledCrossChainBlockData == null means no cross chain data filled in this block.
            if (filledCrossChainBlockData != null)
            {
                generatedTransactions.Add(GenerateNotSignedTransaction(from, CrossChainConsts.CrossChainIndexingMethodName, refBlockNumber,
                    previousBlockPrefix, filledCrossChainBlockData));
            }
            
            return generatedTransactions;
        }

        public void GenerateTransactions(Address @from, long preBlockHeight, Hash previousBlockHash,
            ref List<Transaction> generatedTransactions)
        {
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