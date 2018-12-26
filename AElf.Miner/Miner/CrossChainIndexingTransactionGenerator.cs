using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Exceptions;
using AElf.Miner.Rpc.Server;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Miner.Miner
{
    public class CrossChainIndexingTransactionGenerator
    {
        private readonly ClientManager _clientManager;
        private readonly ServerManager _serverManager;

        public CrossChainIndexingTransactionGenerator(ClientManager clientManager, ServerManager serverManager)
        {
            _clientManager = clientManager;
            _serverManager = serverManager;
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        public async Task<Transaction> GenerateTransactionForIndexingSideChain(Address from, ulong refBlockNumber, byte[] refBlockPrefix)
        {
            var sideChainBlockInfos = await CollectSideChainIndexedInfo();
            if (sideChainBlockInfos.Length == 0)
                return null;
            return GenerateNotSignedTransaction(from, ContractHelpers.IndexingSideChainMethodName, refBlockNumber, refBlockPrefix,
                new object[]{sideChainBlockInfos});
        }
        
        /// <summary>
        /// Generate system txs for parent chain block info and broadcast it.
        /// </summary>
        /// <returns></returns>
        public async Task<Transaction> GenerateTransactionForIndexingParentChain(Address from, ulong refBlockNumber, byte[] refBlockPrefix)
        {
            var parentChainBlockInfo = await CollectParentChainBlockInfo();
            if (parentChainBlockInfo != null && parentChainBlockInfo.Length != 0)
                 return GenerateNotSignedTransaction(from, ContractHelpers.IndexingParentChainMethodName, refBlockNumber, refBlockPrefix,
                    new object[]{parentChainBlockInfo});
            return null;
        }

        /// <summary>
        /// Create a txn with provided data.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="refBlockNumber"></param>
        /// <param name="refBlockPrefix"></param>
        /// <param name="params"></param>
        /// <returns></returns>
        private Transaction GenerateNotSignedTransaction(Address from, String methodName, ulong refBlockNumber, byte[] refBlockPrefix, object[] @params)
        {
            var tx = new Transaction
            {
                From = from,
                To = ContractHelpers.GetCrossChainContractAddress(Hash.LoadBase58(ChainConfig.Instance.ChainId)),
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
            return (await _clientManager.CollectSideChainBlockInfo()).ToArray();
        }

        /// <summary>
        /// Get parent chain block info.
        /// </summary>
        /// <returns></returns>
        private async Task<ParentChainBlockInfo[]> CollectParentChainBlockInfo()
        {
            try
            {
                var blocInfo = await _clientManager.TryGetParentChainBlockInfo();
                return blocInfo?.ToArray();
            }
            catch (Exception e)
            {
                if (e is ClientShutDownException)
                    return null;
                throw;
            }
        }

        /// <summary>
        /// Stop mining
        /// </summary>
        public void Close()
        {
            _clientManager.CloseClientsToSideChain();
            _serverManager.Close();
        }
    }
}