using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public sealed class BlockchainStateManagerTests : AElfKernelTestBase
    {
        private readonly BlockchainStateManager _blockchainStateManager;

        private List<TestPair> _tv;

        public BlockchainStateManagerTests()
        {
            _blockchainStateManager = GetRequiredService<BlockchainStateManager>();
            _tv = new List<TestPair>();
            for (var i = 0; i < 200; i++)
            {
                _tv.Add(new TestPair()
                {
                    BlockHash = Hash.FromRawBytes(new []{Convert.ToByte(i)}),
                    BlockHeight = i,
                    Key = $"key{i}",
                    Value = ByteString.CopyFromUtf8($"value{i}")
                });
            }
        }


        class TestPair
        {
            public Hash BlockHash;
            public long BlockHeight;
            public string Key;
            public ByteString Value;
        }

        [Fact]
        public async Task TestAddBlockStateSet()
        {
            // one level tests without best chain state
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
                    },
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

            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
                    },
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

            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
                    },
                }
            });
            var check3_1 = new Func<Task>(async () =>
            {
                (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[3].BlockHeight, _tv[3].BlockHash))
                    .ShouldBe(_tv[4].Value);
            });


            await check1();
            await check2();
            await check3_1();

            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
                    },
                }
            });

            var check3_2 = new Func<Task>(async () =>
            {
                (await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[3].BlockHeight, _tv[4].BlockHash))
                    .ShouldBe(_tv[5].Value);
            });

            await check1();
            await check2();
            await check3_2();

            int chainId = 1;
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);

            await check1();
            await check2();
            await check3_1();
            await check3_2();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[3].BlockHash));

            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[2].BlockHash);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await check1());
            await check2();

            //test failed merge recover
            chainStateInfo.Status = ChainStateMergingStatus.Merging;
            chainStateInfo.MergingBlockHash = _tv[4].BlockHash;
            
            //merge best to second branch
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[4].BlockHash);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await check1());
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await check2());
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await check3_1());
            await check3_2();
            
            //test failed merge recover
            chainStateInfo.Status = ChainStateMergingStatus.Merged;
            chainStateInfo.MergingBlockHash = _tv[4].BlockHash;
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[4].BlockHash);

        }

        [Fact]
        public async Task Test_MergeBlockState_WithStatus_NotCommonStatus()
        {
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
            {
                BlockHash = _tv[1].BlockHash,
                BlockHeight = _tv[1].BlockHeight,
                PreviousHash = null,
                Changes =
                {
                    {
                        _tv[1].Key,
                        _tv[1].Value
                    }
                }
            });
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.Status = ChainStateMergingStatus.Merged;

            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);
        }
        
        [Fact]
        public async Task Test_GetState_From_VersionedState()
        {
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
            {
                BlockHash = _tv[1].BlockHash,
                BlockHeight = _tv[1].BlockHeight,
                PreviousHash = null,
            });
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
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
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[2].BlockHash);
            
            var result = await _blockchainStateManager.GetStateAsync(_tv[1].Key, _tv[3].BlockHeight, _tv[3].BlockHash);
            result.ShouldNotBeNull();
            result.ShouldBe(_tv[2].Value);
        }

        [Fact]
        public async Task Test_GetState_From_BlockStateSet()
        {
            await _blockchainStateManager.SetBlockStateSetAsync(new BlockStateSet()
            {
                BlockHash = _tv[1].BlockHash,
                BlockHeight = _tv[1].BlockHeight,
                PreviousHash = null,
            });
            var result = await _blockchainStateManager.GetStateAsync(_tv[2].Key, _tv[2].BlockHeight, _tv[1].BlockHash);
            result.ShouldBeNull();
        }
        
        [Fact]
        public async Task TestState_MergedSituation()
        {
            int chainId = 1;
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.Status = ChainStateMergingStatus.Merged;
            chainStateInfo.MergingBlockHash = _tv[1].BlockHash;
            await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, _tv[1].BlockHash);
            
            chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.Status.ShouldBe(ChainStateMergingStatus.Common);
            chainStateInfo.MergingBlockHash.ShouldBeNull();
        }
    }
}