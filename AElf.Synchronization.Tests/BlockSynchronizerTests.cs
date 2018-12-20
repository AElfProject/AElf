using System;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Moq;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public abstract class BlockSyncTestBase : IDisposable
    {
        public static Miners GetRandomMiners()
        {
            Miners m = new Miners();
            m.PublicKeys.Add("04dfd983a2f6831ac0d75ced5a357a921eef69d467dd71bda3c0fdbd3c8f3b8de0fdb403317da85a9bd04c00f8b3e69815badc3dd752aa7aead10561567e8be49b");
            m.PublicKeys.Add("04083f9253ce568a6afeccb582434e003d507f09633178ade1b63358a4f64c7306f7ed29494ee5fefb26178d3b0bc961dfcd180e8ebb5bfdd4ba7432372c251f3f");
            m.PublicKeys.Add("04fd797631e1c07bb0f519b24775d414920fddcdfc433f058aef797564c558a08a672efd4ccb037a4b85fb2fa73808d5cde6a31e35cdaee5f8180d1f5d48fb9b93");
            return m;
        }
        
        // chain service and block chain
        protected Mock<IBlockChain> MockChain;
        protected Mock<IChainService> MockChainService;
        
        // miners 
        protected Mock<IMinersManager> MockMinersManager;
        
        // validation service
        protected Mock<IBlockValidationService> ValidationService;
        
        // executor
        protected Mock<IBlockExecutor> BlockExecutor;
        
        public IBlock Genesis { get; private set; }
        public BlockSynchronizer Synchronizer { get; private set; }
        
        protected BlockSyncTestBase() { }

        // Genesis block with random miners, no validation, exec is success.
        public void GenesisChainSetup()
        {
            SetupGenesisChain();
            SetupMiners();
            
            // Validation service 
            ValidationService = new Mock<IBlockValidationService>();
            ValidationService.Setup(vs => vs.ValidateBlockAsync(It.IsAny<IBlock>(), It.IsAny<IChainContext>()))
                .ReturnsAsync(BlockValidationResult.Success);
            
            // Block executor
            BlockExecutor = new Mock<IBlockExecutor>();
            BlockExecutor.Setup(be => be.ExecuteBlock(It.IsAny<IBlock>())).ReturnsAsync(BlockExecutionResult.Success);
            
            Synchronizer = new BlockSynchronizer(MockChainService.Object, ValidationService.Object, BlockExecutor.Object, MockMinersManager.Object, null);
        }

        private void SetupMiners()
        {
            MockMinersManager = new Mock<IMinersManager>();
            MockMinersManager.Setup(m => m.GetMiners()).ReturnsAsync(GetRandomMiners());
        }

        private void SetupGenesisChain()
        {
            Genesis = SyncTestHelpers.GetGenesisBlock();
            
            // Setup blockchain 
            MockChain = new Mock<IBlockChain>();
            MockChain.Setup(b => b.GetCurrentBlockHeightAsync()).ReturnsAsync(1UL);
            MockChain.Setup(b => b.GetBlockByHeightAsync(It.IsAny<ulong>())).ReturnsAsync(Genesis);
                
            // Setup chain
            MockChainService = new Mock<IChainService>();
            MockChainService.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockChain.Object);
        }

        public void Dispose()
        {
        }
    }
    
    public class BlockSynchronizerBlockSyncTest : BlockSyncTestBase
    {
        [Fact]
        public void InitTest()
        {
            GenesisChainSetup();
            Synchronizer.Init();
            
            Assert.Equal(Synchronizer.HeadBlock.BlockHash, Genesis.GetHash());
        }
        
        [Fact]
        public async Task HandleNewBlock_OnNextBlock_ShouldIncreasHead()
        {
            GenesisChainSetup();
            Synchronizer.Init();

            IBlock block = SyncTestHelpers.BuildNext(Genesis);
            await Synchronizer.TryPushBlock(block);
            
            Assert.Equal(Synchronizer.HeadBlock.BlockHash, block.GetHash());
        }
    }
}