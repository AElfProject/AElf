using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Crosschain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IAccountService _accountService;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService, IAccountService accountService)
        {
            _crossChainService = crossChainService;
            _accountService = accountService;
        }

        public async Task FillExtraData(Block block)
        {
            var crossChainBlockData = new CrossChainBlockData();
            if(block.Header.BlockExtraData == null)
                block.Header.BlockExtraData = new BlockExtraData();
            var sideChainBlockData = await _crossChainService.GetSideChainBlockData();
            var sideChainTransactionsRoot = new BinaryMerkleTree()
                .AddNodes(sideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray()).ComputeRootHash();
            block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            var parentChainBlockData = await _crossChainService.GetParentChainBlockData();
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            
            // append transaction in block for cross chain data sync, it won't be executed.
            var txn = new Transaction
            {
                From = await _accountService.GetAccountAsync(),
                To = Address.Zero,
                MethodName = "RecordCrossChainData",
                Params = crossChainBlockData.ToByteString(),
                Time = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            var rawSig = await _accountService.SignAsync(txn.ToByteArray());
            txn.Sigs.Add(ByteString.CopyFrom(rawSig));
            block.AddTransaction(txn);
        }

        public async Task<bool> ValidateExtraData(Block block)
        {
            try
            {
                var lastTxn = block.Body.TransactionList.Last();
                var crossChainBlockData = CrossChainBlockData.Parser.ParseFrom(lastTxn.Params);
                if (block.Header.BlockExtraData.SideChainTransactionsRoot != null)
                {
                    var calculatedSideChainTransactionsRoot = new BinaryMerkleTree()
                        .AddNodes(crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray())
                        .ComputeRootHash();
                    if (!calculatedSideChainTransactionsRoot.Equals(block.Header.BlockExtraData
                        .SideChainTransactionsRoot))
                        return false;
                }
                return await _crossChainService.ValidateSideChainBlockData(crossChainBlockData.SideChainBlockData) &&
                       await _crossChainService.ValidateParentChainBlockData(crossChainBlockData.ParentChainBlockData);

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}