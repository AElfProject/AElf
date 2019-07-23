using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Xunit;

namespace AElf.Kernel.Blockchain.Domain
{
    public static class BlockNumberExtensions
    {
        public static long BlockHeight(this int index)
        {
            return Constants.GenesisBlockHeight + index;
        }
    }

    public class ChainManagerTests : AElfKernelTestBase
    {
        private readonly ChainManager _chainManager;
        private readonly Hash _genesis;
        private readonly Hash[] _blocks = Enumerable.Range(1, 101).Select(IntToHash).ToArray();

        private static Hash IntToHash(int n)
        {
            var bytes = BitConverter.GetBytes(n);
            var arr = new byte[32];
            Array.Copy(bytes, arr, bytes.Length);
            return Hash.FromByteArray(arr);
        }

        public ChainManagerTests()
        {
            _chainManager = GetRequiredService<ChainManager>();
            _genesis = _blocks[0];
        }

        [Fact]
        public async Task Create_Chain_Success()
        {
            var chain = await _chainManager.GetAsync();
            chain.ShouldBeNull();
            
            var createChainResult = await _chainManager.CreateAsync(_genesis);
            chain = await _chainManager.GetAsync();
            chain.ShouldBe(createChainResult);
            chain.LongestChainHeight.ShouldBe(Constants.GenesisBlockHeight);
            chain.LongestChainHash.ShouldBe(_genesis);
            chain.BestChainHash.ShouldBe(_genesis);
            chain.BestChainHeight.ShouldBe(Constants.GenesisBlockHeight);
            chain.GenesisBlockHash.ShouldBe(_genesis);
            chain.LastIrreversibleBlockHash.ShouldBe(_genesis);
            chain.LastIrreversibleBlockHeight.ShouldBe(Constants.GenesisBlockHeight);
            chain.Branches.Count.ShouldBe(1);
            chain.Branches[_genesis.ToStorageKey()].ShouldBe(Constants.GenesisBlockHeight);

            var blockLink = await _chainManager.GetChainBlockLinkAsync(_genesis);
            blockLink.BlockHash.ShouldBe(_genesis);
            blockLink.Height.ShouldBe(Constants.GenesisBlockHeight);
            blockLink.PreviousBlockHash.ShouldBe(Hash.Empty);
            blockLink.IsLinked.ShouldBeTrue();
            blockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionNone);

            var chainBlockIndex = await _chainManager.GetChainBlockIndexAsync(Constants.GenesisBlockHeight);
            chainBlockIndex.BlockHash.ShouldBe(_genesis);
        }

        [Fact]
        public async Task Create_Chain_ThrowInvalidOperationException()
        {
            await _chainManager.CreateAsync(_genesis);

            await _chainManager.CreateAsync(_genesis).ShouldThrowAsync<InvalidOperationException>();
            await _chainManager.CreateAsync(_blocks[1]).ShouldThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task LIB_Blocks_Test()
        {
            //0 -> 1 linked
            //0 -> 1 -> 5 equals to 0[0] -> 1[1] -> 5[2]
            //not linked: (2) -> 3[5] means a block hash 3 at height 5, has a hash 2 previous block,
            //            but hash 2 block was not in the chain
            //*10, *11[12] means just added to the chain


            var chain = await _chainManager.CreateAsync(_genesis);

            //0 -> *1, no branch
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 1.BlockHeight(),
                    BlockHash = _blocks[1],
                    PreviousBlockHash = _genesis
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(1.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[1]);
            }

            //0 -> 1 -> *2, no branch

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 2.BlockHeight(),
                    BlockHash = _blocks[2],
                    PreviousBlockHash = _blocks[1]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }


            {
                (await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[1])).ShouldBeTrue();
                //test repeat set
                (await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[1])).ShouldBeTrue();
                (await _chainManager.GetChainBlockIndexAsync(0.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(1.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[1]);
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHeight.ShouldBe(1.BlockHeight());
            }

            //0 -> 1 -> 2, no branch
            //not linked: *4

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 4.BlockHeight(),
                    BlockHash = _blocks[4],
                    PreviousBlockHash = _blocks[3]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }

            {
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[4])
                    .ShouldThrowAsync<InvalidOperationException>();
                (await _chainManager.GetChainBlockIndexAsync(0.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(1.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[1]);
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHeight.ShouldBe(1.BlockHeight());
            }

            //0 -> 1 -> 2, no branch
            //not linked: 4, *5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[5],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2 -> *3 -> 4 -> 5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 3.BlockHeight(),
                    BlockHash = _blocks[3],
                    PreviousBlockHash = _blocks[2]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(5.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[5]);
            }

            {
                
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHeight.ShouldBe(1.BlockHeight());

                (await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[4])).ShouldBeTrue();
                (await _chainManager.GetChainBlockIndexAsync(0.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(1.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[1]);
                (await _chainManager.GetChainBlockIndexAsync(2.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[2]);
                (await _chainManager.GetChainBlockIndexAsync(3.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[3]);
                (await _chainManager.GetChainBlockIndexAsync(4.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[4]);


                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[4]);
                chain.LastIrreversibleBlockHeight.ShouldBe(4.BlockHeight());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 , 2 branches
            //                    4 -> *6
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[6],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(5.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> *7
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6.BlockHeight(),
                    BlockHash = _blocks[7],
                    PreviousBlockHash = _blocks[6]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);
            }


            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> *8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[8],
                    PreviousBlockHash = _blocks[9]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> *10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6.BlockHeight(),
                    BlockHash = _blocks[10],
                    PreviousBlockHash = _blocks[5]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] , (11) -> *12[8]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 8.BlockHeight(),
                    BlockHash = _blocks[12],
                    PreviousBlockHash = _blocks[11]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
                chain.NotLinkedBlocks[_blocks[11].ToStorageKey()].ShouldBe(_blocks[12].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6] -> *11[7] -> 12[8]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 7.BlockHeight(),
                    BlockHash = _blocks[11],
                    PreviousBlockHash = _blocks[10]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(8.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[12]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
                chain.NotLinkedBlocks.ContainsKey(_blocks[11].ToStorageKey()).ShouldBeFalse();
            }

            {
                (await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[12])).ShouldBeTrue();
                (await _chainManager.GetChainBlockIndexAsync(0.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(1.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[1]);
                (await _chainManager.GetChainBlockIndexAsync(2.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[2]);
                (await _chainManager.GetChainBlockIndexAsync(3.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[3]);
                (await _chainManager.GetChainBlockIndexAsync(4.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[4]);
                (await _chainManager.GetChainBlockIndexAsync(8.BlockHeight())).BlockHash.ShouldBe(
                    _blocks[12]);


                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[12]);
                chain.LastIrreversibleBlockHeight.ShouldBe(8.BlockHeight());
            }
            
            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6] -> *11[7] -> 12[8]         , 3 branches
            //                                        11[7] -> 13[8] -> 14[9] 
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 8.BlockHeight(),
                    BlockHash = _blocks[13],
                    PreviousBlockHash = _blocks[11]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);
                
                status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 9.BlockHeight(),
                    BlockHash = _blocks[14],
                    PreviousBlockHash = _blocks[13]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);
                
                (await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[14])).ShouldBeFalse();
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[12]);
                chain.LastIrreversibleBlockHeight.ShouldBe(8.BlockHeight());
                
                (await _chainManager.GetChainBlockLinkAsync(_blocks[13])).IsIrreversibleBlock.ShouldBeFalse();
                (await _chainManager.GetChainBlockLinkAsync(_blocks[14])).IsIrreversibleBlock.ShouldBeFalse();
            }
            
            
        }

        [Fact]
        public async Task Should_Attach_Blocks_To_A_Chain()
        {
            //0 -> 1 linked
            //0 -> 1 -> 5 equals to 0[0] -> 1[1] -> 5[2]
            //not linked: (2) -> 3[5] means a block hash 3 at height 5, has a hash 2 previous block,
            //            but hash 2 block was not in the chain
            //*10, *11[12] means just added to the chain


            var chain = await _chainManager.CreateAsync(_genesis);


            //0 -> *1, no branch
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 1.BlockHeight(),
                    BlockHash = _blocks[1],
                    PreviousBlockHash = _genesis
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(1.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[1]);
            }

            //0 -> 1 -> *2, no branch

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 2.BlockHeight(),
                    BlockHash = _blocks[2],
                    PreviousBlockHash = _blocks[1]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2, no branch
            //not linked: *4

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 4.BlockHeight(),
                    BlockHash = _blocks[4],
                    PreviousBlockHash = _blocks[3]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2, no branch
            //not linked: 4, *5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[5],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(2.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2 -> *3 -> 4 -> 5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 3.BlockHeight(),
                    BlockHash = _blocks[3],
                    PreviousBlockHash = _blocks[2]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(5.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 , 2 branches
            //                    4 -> *6
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[6],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(5.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> *7
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6.BlockHeight(),
                    BlockHash = _blocks[7],
                    PreviousBlockHash = _blocks[6]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);
            }


            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> *8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5.BlockHeight(),
                    BlockHash = _blocks[8],
                    PreviousBlockHash = _blocks[9]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> *10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6.BlockHeight(),
                    BlockHash = _blocks[10],
                    PreviousBlockHash = _blocks[5]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] , (11) -> *12[8]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 8.BlockHeight(),
                    BlockHash = _blocks[12],
                    PreviousBlockHash = _blocks[11]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(6.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
                chain.NotLinkedBlocks[_blocks[11].ToStorageKey()].ShouldBe(_blocks[12].ToStorageKey());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6] -> *11[7] -> 12[8]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 7.BlockHeight(),
                    BlockHash = _blocks[11],
                    PreviousBlockHash = _blocks[10]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.LongestChainHeight.ShouldBe(8.BlockHeight());
                chain.LongestChainHash.ShouldBe(_blocks[12]);

                chain.NotLinkedBlocks[_blocks[9].ToStorageKey()].ShouldBe(_blocks[8].ToStorageKey());
                chain.NotLinkedBlocks.ContainsKey(_blocks[11].ToStorageKey()).ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Test_Set_Block_Executed()
        {
            var firstBlockLink = new ChainBlockLink
            {
                Height = 1.BlockHeight(),
                BlockHash = _blocks[1],
                ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionNone
            };

            await _chainManager.SetChainBlockLinkExecutionStatus(firstBlockLink,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);
            var currentBlockLink = await _chainManager.GetChainBlockLinkAsync(_blocks[1]);
            currentBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionSuccess);

            var secondBlockLink = new ChainBlockLink
            {
                Height = 2.BlockHeight(),
                BlockHash = _blocks[2],
                ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionSuccess
            };

            _chainManager
                .SetChainBlockLinkExecutionStatus(secondBlockLink, ChainBlockLinkExecutionStatus.ExecutionSuccess)
                .ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public async Task Set_Block_Validated_Test()
        {
            var firstBlockLink = new ChainBlockLink
            {
                Height = 1.BlockHeight(),
                BlockHash = _blocks[1],
                ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionNone
            };

            await _chainManager.SetChainBlockLinkExecutionStatus(firstBlockLink,
                ChainBlockLinkExecutionStatus.ExecutionFailed);
            var currentBlockLink = await _chainManager.GetChainBlockLinkAsync(_blocks[1]);
            currentBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionFailed);

            var secondBlockLink = new ChainBlockLink
            {
                Height = 2.BlockHeight(),
                BlockHash = _blocks[2],
                ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionFailed
            };

            _chainManager.SetChainBlockLinkExecutionStatus(secondBlockLink, ChainBlockLinkExecutionStatus.ExecutionFailed)
                .ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public async Task Set_Best_Chain_Test()
        {
            var chain = await _chainManager.CreateAsync(_genesis);

            await _chainManager.SetBestChainAsync(chain, 1.BlockHeight(), _blocks[1]);
            var currentChain = await _chainManager.GetAsync();
            currentChain.BestChainHeight.ShouldBe(1.BlockHeight());
            currentChain.BestChainHash.ShouldBe(_blocks[1]);

            _chainManager.SetBestChainAsync(chain, 0.BlockHeight(), _blocks[1])
                .ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public async Task Get_Not_ExecutedBlocks_Test()
        {
            // execution success blocks
            var chain = await _chainManager.CreateAsync(_genesis);
            await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 1.BlockHeight(),
                BlockHash = _blocks[1],
                PreviousBlockHash = _genesis
            });
            await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 2.BlockHeight(),
                BlockHash = _blocks[2],
                PreviousBlockHash = _blocks[1]
            });

            var chainBlockLinks = await _chainManager.GetNotExecutedBlocks(_blocks[2]);
            chainBlockLinks.Count.ShouldBe(3);
            chainBlockLinks[0].BlockHash.ShouldBe(_blocks[0]);
            chainBlockLinks[1].BlockHash.ShouldBe(_blocks[1]);
            chainBlockLinks[2].BlockHash.ShouldBe(_blocks[2]);

            // execution failed block
            await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 3.BlockHeight(),
                BlockHash = _blocks[3],
                PreviousBlockHash = _blocks[2],
            });

            await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 4.BlockHeight(),
                BlockHash = _blocks[4],
                PreviousBlockHash = _blocks[3],
                ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionFailed
            });

            await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 5.BlockHeight(),
                BlockHash = _blocks[5],
                PreviousBlockHash = _blocks[4]
            });


            //when block 3 is the last one, all blocks status is execution none
            chainBlockLinks = await _chainManager.GetNotExecutedBlocks(_blocks[3]);
            chainBlockLinks.Count.ShouldBe(4);
            chainBlockLinks[0].BlockHash.ShouldBe(_blocks[0]);
            chainBlockLinks[1].BlockHash.ShouldBe(_blocks[1]);
            chainBlockLinks[2].BlockHash.ShouldBe(_blocks[2]);


            //when block 5 is the last one, as block 4 is executed failed, mean all block 4's previous blocks have been
            //executed, and as block 4 is failed, so all block after block 4 is failed. so Count = 0
            chainBlockLinks = await _chainManager.GetNotExecutedBlocks(_blocks[5]);
            chainBlockLinks.Count.ShouldBe(0);
        }

        [Fact]
        public async Task Attach_Forked_Blocks_Test()
        {
            // execution success blocks
            var chain = await _chainManager.CreateAsync(_genesis);

            // *4[4]
            var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 4.BlockHeight(),
                BlockHash = _blocks[4],
                PreviousBlockHash = _blocks[3]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);

            // *1[1] ... 4[4]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 1.BlockHeight(),
                BlockHash = _blocks[1],
                PreviousBlockHash = _genesis
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

            // 1[1] ... 4[4] -> *5[5]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 5.BlockHeight(),
                BlockHash = _blocks[5],
                PreviousBlockHash = _blocks[4]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);

            // 1[1] -> *2[2] .... 4[4] -> 5[5]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 2.BlockHeight(),
                BlockHash = _blocks[2],
                PreviousBlockHash = _blocks[1]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

            // 1[1] -> 2[2] -> *3[3] -> 4[4] -> 5[5]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 3.BlockHeight(),
                BlockHash = _blocks[3],
                PreviousBlockHash = _blocks[2]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
            status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

            // Attach 4 again
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 4.BlockHeight(),
                BlockHash = _blocks[4],
                PreviousBlockHash = _blocks[3]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);

            //  1[1] -> 2[2] -> 3[3] -> 4[4] -> 5[5]
            //                             | -> *10[5]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 5.BlockHeight(),
                BlockHash = _blocks[10],
                PreviousBlockHash = _blocks[4]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);

            // 1[1] -> 2[2] -> 3[3] -> 4[4] -> 5[5] -> *6[6]
            //                            | -> 10[5]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 6.BlockHeight(),
                BlockHash = _blocks[6],
                PreviousBlockHash = _blocks[5]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

            // 1[1] -> 2[2] -> 3[3] -> 4[4] -> 5[5] -> 6[6]
            //                            | -> 10[5] -> *11[6]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 6.BlockHeight(),
                BlockHash = _blocks[11],
                PreviousBlockHash = _blocks[10]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.LongestChainFound);

            // 1[1] -> 2[2] -> 3[3] -> 4[4] -> 5[5] -> 6[6] -> *7[7]
            //                            | -> 10[5] -> 11[6]
            status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = 7.BlockHeight(),
                BlockHash = _blocks[7],
                PreviousBlockHash = _blocks[6]
            });
            status.ShouldHaveFlag(BlockAttachOperationStatus.LongestChainFound);
            status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);
        }
    }
}