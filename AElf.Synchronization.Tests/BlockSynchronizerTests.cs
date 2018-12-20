using System;
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
    public class BlockSynchronizerTests
    {
        public Miners GetRandomMiners()
        {
            Miners m = new Miners();
            m.PublicKeys.Add("04dfd983a2f6831ac0d75ced5a357a921eef69d467dd71bda3c0fdbd3c8f3b8de0fdb403317da85a9bd04c00f8b3e69815badc3dd752aa7aead10561567e8be49b");
            m.PublicKeys.Add("04083f9253ce568a6afeccb582434e003d507f09633178ade1b63358a4f64c7306f7ed29494ee5fefb26178d3b0bc961dfcd180e8ebb5bfdd4ba7432372c251f3f");
            m.PublicKeys.Add("04fd797631e1c07bb0f519b24775d414920fddcdfc433f058aef797564c558a08a672efd4ccb037a4b85fb2fa73808d5cde6a31e35cdaee5f8180d1f5d48fb9b93");
            return m;
        }

        [Fact]
        public void InitTest()
        {
            Mock<IMinersManager> mockMinerManager = new Mock<IMinersManager>();
            mockMinerManager.Setup(m => m.GetMiners()).ReturnsAsync(GetRandomMiners());

            IBlock genesis = SyncTestHelpers.GetGenesisBlock();
            
            // Setup blockchain 
            Mock<IBlockChain> mockChain = new Mock<IBlockChain>();
            mockChain.Setup(b => b.GetCurrentBlockHeightAsync()).ReturnsAsync(1UL);
            mockChain.Setup(b => b.GetBlockByHeightAsync(It.IsAny<ulong>())).ReturnsAsync(genesis);
                
            // Setup chain
            Mock<IChainService> chainService = new Mock<IChainService>();
            chainService.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(mockChain.Object);
            
            // Validation service 
            Mock<IBlockValidationService> validationService = new Mock<IBlockValidationService>();
            validationService.Setup(vs => vs.ValidateBlockAsync(It.IsAny<IBlock>(), It.IsAny<IChainContext>()))
                .ReturnsAsync(BlockValidationResult.Success);
            
            // Block executor
            Mock<IBlockExecutor> blockExecutor = new Mock<IBlockExecutor>();
            blockExecutor.Setup(be => be.ExecuteBlock(It.IsAny<IBlock>())).ReturnsAsync(BlockExecutionResult.Success);
            
            // IChainService chainService, IBlockValidationService blockValidationService, IBlockExecutor blockExecutor, IMinersManager minersManager, ILogger logger
            BlockSynchronizer blockSynchronizer = new BlockSynchronizer(chainService.Object, validationService.Object, blockExecutor.Object, mockMinerManager.Object, null);
            blockSynchronizer.Init();
            
            Assert.Equal(blockSynchronizer.Head.BlockHash, genesis.GetHash());
        }
        
        [Fact]
        public void HandleNewBlock_OnNextBlock_ShouldIncreasHead()
        {

        }
    }
}