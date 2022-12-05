using System;
using System.Collections.Generic;

namespace AElf.Kernel.SmartContract.Domain;

[Trait("Category", AElfBlockchainModule)]
public sealed class BlockchainStateManagerTests : AElfKernelTestBase
{
    private readonly IBlockchainStateManager _blockchainStateManager;
    private readonly IBlockStateSetManger _blockStateSetManger;

    private readonly List<TestPair> _tv;

    public BlockchainStateManagerTests()
    {
        _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
        _tv = new List<TestPair>();
        for (var i = 0; i < 200; i++)
            _tv.Add(new TestPair
            {
                BlockHash = HashHelper.ComputeFrom(new[] { Convert.ToByte(i) }),
                BlockHeight = i,
                Key = $"key{i}",
                Value = ByteString.CopyFromUtf8($"value{i}")
            });
    }

    [Fact]
    public async Task AddBlockStateSet_Test()
    {
        // one level tests without best chain state
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            PreviousHash = null,
            Changes =
            {
                {
                    _tv[1].Key,
                    _tv[1].Value
                },
                {
                    _tv[2].Key,
                    _tv[2].Value
                }
            }
        });


        var check1 = new Func<Task>(async () =>
        {
            (await _blockchainStateManager.GetStateAsync(_tv[1].Key, _tv[1].BlockHeight, _tv[1].BlockHash))
                .ShouldBe(_tv[1].Value);

            (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[1].BlockHeight, _tv[1].BlockHash))
                .ShouldBe(_tv[2].Value);
        });

        await check1();

        //two level tests without best chain state

        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[2].BlockHash,
            BlockHeight = _tv[2].BlockHeight,
            PreviousHash = _tv[1].BlockHash,
            Changes =
            {
                //override key 1
                {
                    _tv[1].Key,
                    _tv[2].Value
                }
            }
        });
        var check2 = new Func<Task>(async () =>
        {
            //key 1 was changed, value was changed to value 2
            (await _blockchainStateManager.GetStateAsync(_tv[1].Key, _tv[2].BlockHeight, _tv[2].BlockHash))
                .ShouldBe(_tv[2].Value);

            (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[2].BlockHeight, _tv[2].BlockHash))
                .ShouldBe(_tv[2].Value);
        });

        await check2();

        //but when we we can get value at the height of block height 1, also block hash 1
        await check1();

        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[3].BlockHash,
            BlockHeight = _tv[3].BlockHeight,
            PreviousHash = _tv[2].BlockHash,
            Changes =
            {
                //override key 2 at height 3
                {
                    _tv[2].Key,
                    _tv[4].Value
                }
            }
        });
        var check31 = new Func<Task>(async () =>
        {
            (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[3].BlockHeight, _tv[3].BlockHash))
                .ShouldBe(_tv[4].Value);
        });


        await check1();
        await check2();
        await check31();

        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[4].BlockHash,
            BlockHeight = _tv[3].BlockHeight, // it's a branch
            PreviousHash = _tv[2].BlockHash,
            Changes =
            {
                //override key 2 at height 3
                {
                    _tv[2].Key,
                    _tv[5].Value
                }
            }
        });

        var check32 = new Func<Task>(async () =>
        {
            (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[3].BlockHeight, _tv[4].BlockHash))
                .ShouldBe(_tv[5].Value);
        });

        await check1();
        await check2();
        await check32();

        var chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

        await check1();
        await check2();
        await check31();
        await check32();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[3].BlockHash));

        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[2].BlockHash);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await check1());
        await check2();

        //test failed merge recover
        chainStateInfo.Status = ChainStateMergingStatus.Merging;
        chainStateInfo.MergingBlockHash = _tv[4].BlockHash;

        //merge best to second branch
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[4].BlockHash);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await check1());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await check2());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await check31());
        await check32();

        //test failed merge recover
        chainStateInfo.Status = ChainStateMergingStatus.Merged;
        chainStateInfo.MergingBlockHash = _tv[4].BlockHash;
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[4].BlockHash);
    }

    [Fact]
    public async Task GetState_From_VersionedState_Test()
    {
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            PreviousHash = null
        });
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[2].BlockHash,
            BlockHeight = _tv[2].BlockHeight,
            PreviousHash = _tv[1].BlockHash,
            Changes =
            {
                {
                    _tv[1].Key,
                    _tv[2].Value
                }
            }
        });
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[3].BlockHash,
            BlockHeight = _tv[3].BlockHeight,
            PreviousHash = null,
            Changes =
            {
                {
                    _tv[2].Key,
                    _tv[3].Value
                }
            }
        });

        var chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[2].BlockHash);

        var result = await _blockchainStateManager.GetStateAsync(_tv[1].Key, _tv[3].BlockHeight, _tv[3].BlockHash);
        result.ShouldNotBeNull();
        result.ShouldBe(_tv[2].Value);
    }

    [Fact]
    public async Task GetState_From_BlockStateSet_Test()
    {
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            PreviousHash = null
        });
        var result = await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[2].BlockHeight, _tv[1].BlockHash);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task State_MergedSituation_Test()
    {
        var chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        chainStateInfo.Status = ChainStateMergingStatus.Merged;
        chainStateInfo.MergingBlockHash = _tv[1].BlockHash;
        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

        chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        chainStateInfo.Status.ShouldBe(ChainStateMergingStatus.Common);
        chainStateInfo.MergingBlockHash.ShouldBeNull();
    }

    [Fact]
    public async Task MergeBlockState_WithStatus_Merging()
    {
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[0].BlockHash,
            BlockHeight = _tv[0].BlockHeight,
            PreviousHash = HashHelper.ComputeFrom("PreviousHash"),
            Changes =
            {
                {
                    _tv[1].Key,
                    _tv[1].Value
                }
            }
        });

        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            PreviousHash = _tv[0].BlockHash,
            Changes =
            {
                {
                    _tv[1].Key,
                    _tv[1].Value
                }
            }
        });

        var chainStateInfo = new ChainStateInfo
        {
            BlockHash = _tv[0].BlockHash,
            BlockHeight = _tv[0].BlockHeight,
            MergingBlockHash = _tv[1].BlockHash,
            Status = ChainStateMergingStatus.Merging
        };

        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

        chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        chainStateInfo.BlockHash.ShouldBe(_tv[1].BlockHash);
        chainStateInfo.BlockHeight.ShouldBe(_tv[1].BlockHeight);
        chainStateInfo.Status.ShouldBe(ChainStateMergingStatus.Common);
        chainStateInfo.MergingBlockHash.ShouldBeNull();

        var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(_tv[1].BlockHash);
        blockStateSet.ShouldBeNull();
    }

    [Fact]
    public async Task MergeBlockState_WithStatus_Merged_WithSet_Removed()
    {
        var chainStateInfo = new ChainStateInfo
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            MergingBlockHash = _tv[1].BlockHash,
            Status = ChainStateMergingStatus.Merged
        };

        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

        chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        chainStateInfo.BlockHash.ShouldBe(_tv[1].BlockHash);
        chainStateInfo.BlockHeight.ShouldBe(_tv[1].BlockHeight);
        chainStateInfo.Status.ShouldBe(ChainStateMergingStatus.Common);
        chainStateInfo.MergingBlockHash.ShouldBeNull();
    }

    [Fact]
    public async Task MergeBlockState_WithStatus_Merged()
    {
        await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            PreviousHash = HashHelper.ComputeFrom("PreviousHash"),
            Changes =
            {
                {
                    _tv[1].Key,
                    _tv[1].Value
                }
            }
        });

        var chainStateInfo = new ChainStateInfo
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            MergingBlockHash = _tv[1].BlockHash,
            Status = ChainStateMergingStatus.Merged
        };

        await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

        chainStateInfo = await _blockStateSetManger.GetChainStateInfoAsync();
        chainStateInfo.BlockHash.ShouldBe(_tv[1].BlockHash);
        chainStateInfo.BlockHeight.ShouldBe(_tv[1].BlockHeight);
        chainStateInfo.Status.ShouldBe(ChainStateMergingStatus.Common);
        chainStateInfo.MergingBlockHash.ShouldBeNull();

        var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(_tv[1].BlockHash);
        blockStateSet.ShouldBeNull();
    }

    [Fact]
    public async Task MergeBlockState_ShouldThrowInvalidOperationException()
    {
        var chainStateInfo = new ChainStateInfo
        {
            BlockHash = _tv[1].BlockHash,
            BlockHeight = _tv[1].BlockHeight,
            MergingBlockHash = null,
            Status = ChainStateMergingStatus.Common
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _blockStateSetManger.MergeBlockStateAsync(chainStateInfo, _tv[2].BlockHash));
    }

    private class TestPair
    {
        public Hash BlockHash;
        public long BlockHeight;
        public string Key;
        public ByteString Value;
    }
}