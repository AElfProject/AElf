using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Crosschain.Exceptions;
using AElf.Kernel;
using AElf.Kernel.Txn;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Crosschain
{
    public class CrossChainIndexingTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly ICrossChainService _crossChainService;

        private delegate void CrossChainTransactionGeneratorDelegate(Address from, ulong refBlockNumber,
            byte[] refBlockPrefix, IEnumerable<Transaction> generatedTransactions);

        private readonly CrossChainTransactionGeneratorDelegate _crossChainTransactionGenerators;

        public CrossChainIndexingTransactionGenerator(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
            _crossChainTransactionGenerators += GenerateTransactionForIndexingSideChain;
            _crossChainTransactionGenerators += GenerateTransactionForIndexingParentChain;
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        private void GenerateTransactionForIndexingSideChain(Address from, ulong refBlockNumber, 
            byte[] refBlockPrefix, IEnumerable<Transaction> generatedTransactions)
        {
//            var sideChainBlockInfos = await CollectSideChainIndexedInfo();
//            if (sideChainBlockInfos.Length == 0)
//                return;
            generatedTransactions.Append(GenerateNotSignedTransaction(from, ContractHelpers.IndexingSideChainMethodName,
                refBlockNumber, refBlockPrefix, new object[0]));
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        private void GenerateTransactionForIndexingParentChain(Address from, ulong refBlockNumber, 
            byte[] refBlockPrefix, IEnumerable<Transaction> generatedTransactions)
        {
            //var parentChainBlockInfo = await CollectParentChainBlockInfo();
            //if (parentChainBlockInfo != null && parentChainBlockInfo.Length != 0)
            generatedTransactions.Append(GenerateNotSignedTransaction(from,
                ContractHelpers.IndexingParentChainMethodName, refBlockNumber, refBlockPrefix, new object[0]));
        }

        public void GenerateTransactions(Address @from, ulong preBlockHeight, ulong refBlockHeight, byte[] refBlockPrefix,
            ref List<Transaction> generatedTransactions)
        {
            _crossChainTransactionGenerators(from, refBlockHeight, refBlockPrefix, generatedTransactions);
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="methodName"></param>
        /// <param name="refBlockNumber"></param>
        /// <param name="refBlockPrefix"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        private Transaction GenerateNotSignedTransaction(Address from, string methodName, ulong refBlockNumber, 
            byte[] refBlockPrefix, object[] @params)
        {
            var tx = new Transaction
            {
                From = from,
                To = ContractHelpers.GetCrossChainContractAddress(ChainConfig.Instance.ChainId.ConvertBase58ToChainId()),
                RefBlockNumber = refBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(refBlockPrefix),
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params)),
                Time = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            return tx;
        }
        
        /// <summary>
        /// Side chains header info    
        /// </summary>
        /// <returns></returns>
        private async Task<SideChainBlockInfo[]> CollectSideChainIndexedInfo()
        {
            // interval waiting for each side chain
            return (await _crossChainService.GetSideChainBlockInfo()).ToArray();
        }

        /// <summary>
        /// Get parent chain block info.
        /// </summary>
        /// <returns></returns>
        private async Task<ParentChainBlockInfo[]> CollectParentChainBlockInfo()
        {
            try
            {
                var blocInfo = await _crossChainService.GetParentChainBlockInfo();
                return blocInfo?.ToArray();
            }
            catch (Exception e)
            {
                if (e is ClientShutDownException)
                    return null;
                throw;
            }
        }

    }
}