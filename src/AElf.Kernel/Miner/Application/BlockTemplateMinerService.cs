using System;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public class BlockTemplateMinerService : IBlockTemplateMinerService, ISingletonDependency
    {
        private Block _blockCache = null;

        private readonly IMinerService _minerService;

        private readonly IAccountService _accountService;

        public BlockTemplateMinerService(IMinerService minerService, IAccountService accountService)
        {
            _minerService = minerService;
            _accountService = accountService;
        }

        public async Task<BlockHeader> CreateTemplateCacheAsync(Hash previousBlockHash, long previousBlockHeight,
            Timestamp blockTime, Duration blockExecutionTime)
        {
            _blockCache =
                await _minerService.MineAsync(previousBlockHash, previousBlockHeight, blockTime, blockExecutionTime);

            _blockCache.Header.Signature = ByteString.Empty;

            return _blockCache.Header;
        }

        public async Task<Block> ChangeTemplateCacheBlockHeaderAndClearCacheAsync(BlockHeader blockHeader)
        {
            var block = _blockCache.Clone();

            if (block == null || block.Header.PreviousBlockHash != blockHeader.PreviousBlockHash ||
                block.Header.MerkleTreeRootOfTransactions != blockHeader.MerkleTreeRootOfTransactions ||
                block.Header.Time != blockHeader.Time)
                throw new InvalidOperationException("template cache not match");
            //same block

            block.Header = blockHeader;
            block.Header.Signature =
                ByteString.CopyFrom(await _accountService.SignAsync(block.GetHash().ToByteArray()));

            block.Body.BlockHeader = blockHeader.GetHash();

            return block;
        }
    }
}