using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Crosschain
{
    public class CrossChainIndexingTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainService _crossChainService;

        private delegate Task CrossChainTransactionGeneratorDelegate(Address from, ulong refBlockNumber,
            byte[] refBlockPrefix, int chainId, IEnumerable<Transaction> generatedTransactions);

        private readonly CrossChainTransactionGeneratorDelegate _crossChainTransactionGenerators;

        public CrossChainIndexingTransactionGenerator(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
            _crossChainTransactionGenerators += GenerateCrossChainIndexingTransaction;
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        private void GenerateTransactionForIndexingSideChain(Address from, ulong refBlockNumber, 
            byte[] refBlockPrefix, int chainId, IEnumerable<Transaction> generatedTransactions)
        {
//            var sideChainBlockInfos = await CollectSideChainIndexedInfo();
//            if (sideChainBlockInfos.Length == 0)
//                return;
            generatedTransactions.Append(GenerateNotSignedTransaction(from, CrossChainConsts.IndexingSideChainMethodName,
                refBlockNumber, refBlockPrefix, new object[0], chainId));
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        private void GenerateTransactionForIndexingParentChain(Address from, ulong refBlockNumber, 
            byte[] refBlockPrefix, int chainId, IEnumerable<Transaction> generatedTransactions)
        {
            //var parentChainBlockData = await CollectParentChainBlockInfo();
            //if (parentChainBlockData != null && parentChainBlockData.Length != 0)
            generatedTransactions.Append(GenerateNotSignedTransaction(from,
                CrossChainConsts.IndexingParentChainMethodName, refBlockNumber, refBlockPrefix, new object[0], chainId));
        }

        private async Task GenerateCrossChainIndexingTransaction(Address from, ulong refBlockNumber, 
            byte[] refBlockPrefix, int chainId, IEnumerable<Transaction> generatedTransactions)
        {
            var crossChainBlockData = new CrossChainBlockData();
            var sideChainBlockData = await _crossChainService.GetSideChainBlockData(chainId);
            
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            var parentChainBlockData = await _crossChainService.GetParentChainBlockData(chainId);
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            generatedTransactions.Append(GenerateNotSignedTransaction(from,
                CrossChainConsts.CrossChainIndexingMethodName, refBlockNumber, refBlockPrefix, new object[]{crossChainBlockData}, chainId));
        }

        public void GenerateTransactions(Address @from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix, int chainId,
            ref List<Transaction> generatedTransactions)
        {
            _crossChainTransactionGenerators(from, refBlockHeight, refBlockPrefix, chainId, generatedTransactions);
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="methodName"></param>
        /// <param name="refBlockNumber"></param>
        /// <param name="chainId"></param>
        /// <param name="refBlockPrefix"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        private Transaction GenerateNotSignedTransaction(Address from, string methodName, ulong refBlockNumber, 
            byte[] refBlockPrefix, object[] @params, int chainId)
        {
            return new Transaction
            {
                From = from,
                To = ContractHelpers.GetCrossChainContractAddress(chainId),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params)),
                Time = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }
}