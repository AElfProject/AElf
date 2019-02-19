using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Tests;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Xunit;

namespace AElf.Kernel.Managers.Another.Tests
{
    public class ChainManagerTests : AElfKernelTestBase
    {
        private readonly ChainManager _chainManager;


        private readonly Hash _genesis;


        private readonly Hash[] _blocks = Enumerable.Range(0, 100)
            .Select(IntToHash).ToArray();

        private static Hash IntToHash(int n)
        {
            var bytes = BitConverter.GetBytes(n);
            var arr = new byte[32];
            Array.Copy(bytes,arr,bytes.Length);
            return Hash.LoadByteArray(arr);
        }

        public ChainManagerTests()
        {
            _chainManager = GetRequiredService<ChainManager>();
            _genesis = _blocks[0];
        }


        [Fact]
        public async Task Should_Create_Chain()
        {
            var chain = await _chainManager.CreateAsync(0, _genesis);
            chain.BestChainHash.ShouldBe(_genesis);
            chain.GenesisBlockHash.ShouldBe(_genesis);
            chain.BestChainHeight.ShouldBe(0u);
        }

        [Fact]
        public async Task Test_LIB_Blocks()
        {
            //0 -> 1 linked
            //0 -> 1 -> 5 equals to 0[0] -> 1[1] -> 5[2]
            //not linked: (2) -> 3[5] means a block hash 3 at height 5, has a hash 2 previous block,
            //            but hash 2 block was not in the chain
            //*10, *11[12] means just added to the chain


            var chain = await _chainManager.CreateAsync(0, _genesis);


            //0 -> *1, no branch
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 1,
                    BlockHash = _blocks[1],
                    PreviousBlockHash = _genesis
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(1u);
                chain.BestChainHash.ShouldBe(_blocks[1]);
            }

            //0 -> 1 -> *2, no branch

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 2,
                    BlockHash = _blocks[2],
                    PreviousBlockHash = _blocks[1]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }


            {
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[1]);
                //test repeat set
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[1]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 0)).BlockHash.ShouldBe(_blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 1)).BlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHeight.ShouldBe(1u);
            }

            //0 -> 1 -> 2, no branch
            //not linked: *4

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 4,
                    BlockHash = _blocks[4],
                    PreviousBlockHash = _blocks[3]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }
            
            {
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[4]).ShouldThrowAsync<InvalidOperationException>();
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 0)).BlockHash.ShouldBe(_blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 1)).BlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[1]);
                chain.LastIrreversibleBlockHeight.ShouldBe(1u);
            }

            //0 -> 1 -> 2, no branch
            //not linked: 4, *5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[5],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2 -> *3 -> 4 -> 5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 3,
                    BlockHash = _blocks[3],
                    PreviousBlockHash = _blocks[2]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(5u);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }
            
            {
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[4]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 0)).BlockHash.ShouldBe(_blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 1)).BlockHash.ShouldBe(_blocks[1]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 2)).BlockHash.ShouldBe(_blocks[2]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 3)).BlockHash.ShouldBe(_blocks[3]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 4)).BlockHash.ShouldBe(_blocks[4]);


                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[4]);
                chain.LastIrreversibleBlockHeight.ShouldBe(4u);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 , 2 branches
            //                    4 -> *6
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[6],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(5u);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> *7
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6,
                    BlockHash = _blocks[7],
                    PreviousBlockHash = _blocks[6]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);
            }


            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> *8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[8],
                    PreviousBlockHash = _blocks[9]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> *10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6,
                    BlockHash = _blocks[10],
                    PreviousBlockHash = _blocks[5]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] , (11) -> *12[8]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 8,
                    BlockHash = _blocks[12],
                    PreviousBlockHash = _blocks[11]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
                chain.NotLinkedBlocks[_blocks[11].ToHex()].ShouldBe(_blocks[12].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6] -> *11[7] -> 12[8]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 7,
                    BlockHash = _blocks[11],
                    PreviousBlockHash = _blocks[10]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(8u);
                chain.BestChainHash.ShouldBe(_blocks[12]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
                chain.NotLinkedBlocks.ContainsKey(_blocks[11].ToHex()).ShouldBeFalse();
            }
            
            {
                await _chainManager.SetIrreversibleBlockAsync(chain, _blocks[12]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 0)).BlockHash.ShouldBe(_blocks[0]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 1)).BlockHash.ShouldBe(_blocks[1]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 2)).BlockHash.ShouldBe(_blocks[2]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 3)).BlockHash.ShouldBe(_blocks[3]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 4)).BlockHash.ShouldBe(_blocks[4]);
                (await _chainManager.GetChainBlockIndexAsync(chain.Id, 8)).BlockHash.ShouldBe(_blocks[12]);


                chain.LastIrreversibleBlockHash.ShouldBe(_blocks[12]);
                chain.LastIrreversibleBlockHeight.ShouldBe(8u);
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


            var chain = await _chainManager.CreateAsync(0, _genesis);


            //0 -> *1, no branch
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 1,
                    BlockHash = _blocks[1],
                    PreviousBlockHash = _genesis
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(1u);
                chain.BestChainHash.ShouldBe(_blocks[1]);
            }

            //0 -> 1 -> *2, no branch

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 2,
                    BlockHash = _blocks[2],
                    PreviousBlockHash = _blocks[1]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2, no branch
            //not linked: *4

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 4,
                    BlockHash = _blocks[4],
                    PreviousBlockHash = _blocks[3]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2, no branch
            //not linked: 4, *5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[5],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(2u);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }

            //0 -> 1 -> 2 -> *3 -> 4 -> 5

            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 3,
                    BlockHash = _blocks[3],
                    PreviousBlockHash = _blocks[2]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(5u);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 , 2 branches
            //                    4 -> *6
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[6],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(5u);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> *7
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6,
                    BlockHash = _blocks[7],
                    PreviousBlockHash = _blocks[6]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);
            }


            //0 -> 1 -> 2 -> 3 -> 4 -> 5         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> *8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[8],
                    PreviousBlockHash = _blocks[9]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> *10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] 
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 6,
                    BlockHash = _blocks[10],
                    PreviousBlockHash = _blocks[5]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5] , (11) -> *12[8]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 8,
                    BlockHash = _blocks[12],
                    PreviousBlockHash = _blocks[11]
                });

                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(6u);
                chain.BestChainHash.ShouldBe(_blocks[7]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
                chain.NotLinkedBlocks[_blocks[11].ToHex()].ShouldBe(_blocks[12].ToHex());
            }

            //0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 10[6] -> *11[7] -> 12[8]         , 2 branches
            //                    4 -> 6 -> 7[6]
            //not linked: (9) -> 8[5]
            {
                var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = 7,
                    BlockHash = _blocks[11],
                    PreviousBlockHash = _blocks[10]
                });

                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttachOperationStatus.BestChainFound);
                status.ShouldHaveFlag(BlockAttachOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttachOperationStatus.NewBlockNotLinked);

                chain.BestChainHeight.ShouldBe(8u);
                chain.BestChainHash.ShouldBe(_blocks[12]);

                chain.NotLinkedBlocks[_blocks[9].ToHex()].ShouldBe(_blocks[8].ToHex());
                chain.NotLinkedBlocks.ContainsKey(_blocks[11].ToHex()).ShouldBeFalse();
            }
        }
    }
}