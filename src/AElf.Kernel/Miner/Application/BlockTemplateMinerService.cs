using System;
using AElf.Kernel.Account.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public class BlockTemplateMinerService : IBlockTemplateMinerService, ISingletonDependency
{
    private readonly IAccountService _accountService;

    private readonly IMinerService _minerService;
    private Block _blockCache;

    public BlockTemplateMinerService(IMinerService minerService, IAccountService accountService)
    {
        _minerService = minerService;
        _accountService = accountService;
    }

    public async Task<BlockHeader> CreateTemplateCacheAsync(Hash previousBlockHash, long previousBlockHeight,
        Timestamp blockTime, Duration blockExecutionTime)
    {
        _blockCache =
            (await _minerService.MineAsync(previousBlockHash, previousBlockHeight, blockTime, blockExecutionTime))
            .Block;

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

        return block;
    }
}