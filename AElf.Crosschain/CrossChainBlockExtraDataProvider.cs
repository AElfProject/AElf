using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.BlockService;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Crosschain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }

        public async Task FillExtraData(Block block)
        {
            var crossChainBlockData = new CrossChainBlockData();
            if(block.Header.BlockExtraData == null)
                block.Header.BlockExtraData = new BlockExtraData();
            var sideChainBlockData = await _crossChainService.GetSideChainBlockInfo();
            var sideChainTransactionsRoot = new BinaryMerkleTree()
                .AddNodes(sideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray()).ComputeRootHash();
            block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
            crossChainBlockData.SideChainBlockData.AddRange(sideChainBlockData);
            var parentChainBlockData = await _crossChainService.GetParentChainBlockInfo();
            crossChainBlockData.ParentChainBlockData.AddRange(parentChainBlockData);
            
            // add a transaction in block for cross chain data sync, it won't be executed.
            block.AddTransaction(new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = "RecordCrossChainData",
                Params = crossChainBlockData.ToByteString(),
                Time = Timestamp.FromDateTime(DateTime.UtcNow)
            });
        }

        public Task<bool> ValidateExtraData(Block block)
        {
            throw new NotImplementedException();
        }
    }
}