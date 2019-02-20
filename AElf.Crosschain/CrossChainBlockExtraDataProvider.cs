using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Crosschain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly ITransactionResultManager _transactionResultManager;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService, ITransactionResultManager transactionResultManager)
        {
            _crossChainService = crossChainService;
            _transactionResultManager = transactionResultManager;
        }

        public async Task FillExtraDataAsync(Block block)
        {
            if (!TryGetLogEventInBlock(block, out var logEvent))
                return;
            foreach (var txId in block.Body.Transactions)
            {
                var res = await _transactionResultManager.GetTransactionResultAsync(txId);
                foreach (var eventLog in res.Logs)
                {
                    if (!logEvent.Topics.Equals(eventLog.Topics))
                        continue;
                    object[] indexingEventData = ExtractCrossChainBlockData(eventLog);
                    block.Header.BlockExtraData.SideChainTransactionsRoot = (Hash) indexingEventData[0];
                    return;
                }
            }
//            var crossChainBlockData = new CrossChainBlockData();
//            if(block.Header.BlockExtraData == null)
//                block.Header.BlockExtraData = new BlockExtraData();
//            var sideChainBlockData = await _crossChainService.GetSideChainBlockData();
//            var sideChainTransactionsRoot = new BinaryMerkleTree()
//                .AddNodes(sideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray()).ComputeRootHash();
//            block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
//            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
//            var parentChainBlockData = await _crossChainService.GetParentChainBlockData();
//            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            
            // append transaction in block for cross chain data sync, it won't be executed.
//            var txn = new Transaction
//            {
//                From = await _accountService.GetAccountAsync(),
//                To = Address.Zero,
//                MethodName = "RecordCrossChainData",
//                Params = crossChainBlockData.ToByteString(),
//                Time = Timestamp.FromDateTime(DateTime.UtcNow)
//            };
//            var rawSig = await _accountService.SignAsync(txn.ToByteArray());
//            txn.Sigs.Add(ByteString.CopyFrom(rawSig));
//            block.AddTransaction(txn);
        }
        public async Task<bool> ValidateExtraDataAsync(Block block)
        {
            try
            {
                if (block.Header.BlockExtraData.SideChainTransactionsRoot == null)
                {
                    return true;
                }

                if (!TryGetLogEventInBlock(block, out var logEvent))
                    return false;
                
                foreach (var txId in block.Body.Transactions)
                {
                    var res = await _transactionResultManager.GetTransactionResultAsync(txId);
                    foreach (var eventLog in res.Logs)
                    {
                        if (!logEvent.Topics.Equals(eventLog.Topics))
                            continue;
                        object[] indexingEventData = ExtractCrossChainBlockData(eventLog);
                        var sideChainTransactionsRoot = (Hash) indexingEventData[0];
                        var crossChainBlockData = (CrossChainBlockData) indexingEventData[1];
                        if (!sideChainTransactionsRoot.Equals(block.Header.BlockExtraData
                                .SideChainTransactionsRoot))
                            return false;
                        return await _crossChainService.ValidateSideChainBlockData(crossChainBlockData.SideChainBlockData) &&
                               await _crossChainService.ValidateParentChainBlockData(crossChainBlockData.ParentChainBlockData);
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        private object[] ExtractCrossChainBlockData(LogEvent logEvent)
        {
            return ParamsPacker.Unpack(logEvent.Data.ToByteArray(), new[] {typeof(Hash), typeof(CrossChainBlockData)});
        }

        private bool TryGetLogEventInBlock(Block block, out LogEvent logEvent)
        {
            logEvent = new LogEvent
            {
                Address = ContractHelpers.GetGenesisBasicContractAddress(block.Header.ChainId),
                Topics =
                {
                    ByteString.CopyFrom(CrossChainConsts.CrossChainIndexingEvent.CalculateHash())
                }
            };
            return logEvent.GetBloom().IsIn(new Bloom(block.Header.Bloom.ToByteArray()));
        }
    }
}