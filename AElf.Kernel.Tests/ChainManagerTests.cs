using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Tests;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Xunit;

namespace AElf.Kernel.Managers.Another.Tests
{
    public class ChainManagerTests : AElfKernelTestBase
    {
        private ChainManager _chainManager;


        private readonly Hash _genesis;


        private readonly Hash[] _blocks = Enumerable.Range(0, 100)
            .Select(p => Hash.Generate()).ToArray();

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
            chain.BestChainHeight.ShouldBe(0);
        }

        [Fact]
        public async Task Should_Attach_Blocks_To_A_Chain()
        {
            var chain = await _chainManager.CreateAsync(0, _genesis);

            
            //0 -> 1, no branch
            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 1,
                    BlockHash = _blocks[1],
                    PreviousBlockHash = _genesis
                });

                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(1);
                chain.BestChainHash.ShouldBe(_blocks[1]);
            }
            
            //0 -> 1 -> 2, no branch

            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 2,
                    BlockHash = _blocks[2],
                    PreviousBlockHash = _blocks[1]
                });

                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(2);
                chain.BestChainHash.ShouldBe(_blocks[2]);

            }
            
            //0 -> 1 -> 2, no branch
            //not linked: 4
            
            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 4,
                    BlockHash = _blocks[4],
                    PreviousBlockHash = _blocks[3]
                });

                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(2);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }
            
            //0 -> 1 -> 2, no branch
            //not linked: 4, 5
            
            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[5],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(2);
                chain.BestChainHash.ShouldBe(_blocks[2]);
            }
            
            //0 -> 1 -> 2 -> 3 -> 4 -> 5
            
            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 3,
                    BlockHash = _blocks[3],
                    PreviousBlockHash = _blocks[2]
                });

                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(5);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }
            
            //0 -> 1 -> 2 -> 3 -> 4 -> 5 , 2 branches
            //                    4 -> 6
            {
                var status = await _chainManager.AttachBlockToChain(chain, new ChainBlockLink()
                {
                    Height = 5,
                    BlockHash = _blocks[6],
                    PreviousBlockHash = _blocks[4]
                });

                status.ShouldHaveFlag(BlockAttchOperationStatus.NewBlockLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.BestChainFound);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlocksLinked);
                status.ShouldNotHaveFlag(BlockAttchOperationStatus.NewBlockNotLinked);
                
                chain.BestChainHeight.ShouldBe(5);
                chain.BestChainHash.ShouldBe(_blocks[5]);
            }

        }
    }
}