using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Easy.MessageHub;
using Moq;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSyncTestBase : IDisposable
    {
        protected Miners Miners;
        protected ECKeyPair CurrentMiner;
        
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

        protected BlockSyncTestBase()
        {
            ChainConfig.Instance.ChainId = "kPBx"; 
        }

        // Genesis block with random miners, no validation, exec is success.
        public void GenesisChainSetup()
        {
            SetupGenesisChain();
            SetupMiners();
            
            // Validation service 
            ValidationService = new Mock<IBlockValidationService>();
            ValidationService.Setup(vs => vs.ValidateBlockAsync(It.IsAny<IBlock>()))
                .ReturnsAsync(BlockValidationResult.Success);
            
            // Block executor
            BlockExecutor = new Mock<IBlockExecutor>();
            BlockExecutor.Setup(be => be.ExecuteBlock(It.IsAny<IBlock>())).ReturnsAsync(BlockExecutionResult.Success);
            
            Synchronizer = new BlockSynchronizer(MockChainService.Object, ValidationService.Object, BlockExecutor.Object, MockMinersManager.Object, null);
        }

        protected void SetupMiners()
        {
            List<ECKeyPair> miners = SyncTestHelpers.GetRandomMiners();

            NodeConfig.Instance.ECKeyPair = miners.ElementAt(0);
            CurrentMiner = miners.ElementAt(0);
            
            Miners = new Miners();
            miners.ForEach(m => Miners.PublicKeys.Add(m.PublicKey.ToHex()));
                
            MockMinersManager = new Mock<IMinersManager>();
            MockMinersManager.Setup(m => m.GetMiners()).ReturnsAsync(Miners);
        }

        private void SetupGenesisChain()
        {
            Genesis = SyncTestHelpers.GetGenesisBlock();
            
            // Setup blockchain 
            MockChain = new Mock<IBlockChain>();
            MockChain.Setup(b => b.GetCurrentBlockHeightAsync()).ReturnsAsync(1UL);
            MockChain.Setup(b => b.GetBlockByHeightAsync(It.IsAny<ulong>(), It.IsAny<bool>())).ReturnsAsync(Genesis);
                
            // Setup chain
            MockChainService = new Mock<IChainService>();
            MockChainService.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockChain.Object);
        }

        protected List<ulong> RollbackToheightCalls = new List<ulong>();
        public void MonitorRollbackToHeightCalls()
        {
            MockChain.Setup(bc => bc.RollbackToHeight(It.IsAny<ulong>()))
                .ReturnsAsync(new List<Transaction>())
                .Callback<ulong>(m => RollbackToheightCalls.Add(m));
        }
        
        protected List<IBlock> ExecuteBlockCalls = new List<IBlock>();
        public void MonitorExecuteBlockCalls()
        {
            BlockExecutor.Setup(be => be.ExecuteBlock(It.IsAny<IBlock>()))
                .ReturnsAsync(BlockExecutionResult.Success)
                .Callback<IBlock>(m => ExecuteBlockCalls.Add(m));
        }

        public void Dispose()
        {
            MessageHub.Instance.ClearSubscriptions();
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
            Assert.Equal(Synchronizer.CurrentLib.BlockHash, Genesis.GetHash());
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

        [Fact]
        public async Task ReceiveSameBlockHeightAfterMined()
        {
            GenesisChainSetup();
            
            Synchronizer.Init();
            
            IBlock forkRoot = SyncTestHelpers.BuildNext(Genesis); // Height 2
            IBlock blockForkA = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            IBlock blockForkB = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            
            await Synchronizer.TryPushBlock(forkRoot);
            Synchronizer.AddMinedBlock(blockForkA); // mined
            await Synchronizer.TryPushBlock(blockForkB); // from net
            
            Assert.Equal(Synchronizer.HeadBlock.BlockHash, blockForkA.GetHash());
        }

        [Fact]
        public async Task HeadBlock_OnExtendForkHigherThanHead_ShouldSwitch()
        {
            GenesisChainSetup();
            
            Synchronizer.Init();

            IBlock forkRoot = SyncTestHelpers.BuildNext(Genesis); // Height 2
            
            IBlock blockForkA = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            IBlock blockForkB = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            
            IBlock blockForkB1 = SyncTestHelpers.BuildNext(blockForkB); // Height 4
            
            await Synchronizer.TryPushBlock(forkRoot);
            await Synchronizer.TryPushBlock(blockForkA);
            await Synchronizer.TryPushBlock(blockForkB);
            
            // A is still current chain
            Assert.Equal(Synchronizer.HeadBlock.BlockHash, blockForkA.GetHash());

            MonitorRollbackToHeightCalls();
            MonitorExecuteBlockCalls();
                
            // B should get longer
            await Synchronizer.TryPushBlock(blockForkB1);
            
            // B1 should be new head 
            Assert.Equal(Synchronizer.HeadBlock.BlockHash, blockForkB1.GetHash());
            Assert.Single(RollbackToheightCalls);
            Assert.Equal(2UL, RollbackToheightCalls.ElementAt(0));
            
            Assert.Equal(2, ExecuteBlockCalls.Count);
            Assert.Equal(ExecuteBlockCalls.ElementAt(0).GetHash(), blockForkB.GetHash());
            Assert.Equal(ExecuteBlockCalls.ElementAt(1).GetHash(), blockForkB1.GetHash());
        }

        [Fact]
        public async Task HeadBlock_OnProducersAllAddedABlock_ShouldMakeNewLib()
        {
            GenesisChainSetup();
            
            Synchronizer.Init();

            string miner1 = Miners.PublicKeys.ElementAt(0);
            string miner2 = Miners.PublicKeys.ElementAt(1);
            string miner3 = Miners.PublicKeys.ElementAt(2);

            IBlock block1 = SyncTestHelpers.BuildNext(Genesis, miner1); // miner 01
            IBlock block2 = SyncTestHelpers.BuildNext(block1, miner2);  // miner 02
            IBlock block3 = SyncTestHelpers.BuildNext(block2, miner3);  // miner 03
            
            // 2 and 3 should confirm the block
            
            await Synchronizer.TryPushBlock(block1);
            await Synchronizer.TryPushBlock(block2);
            
            // check that the current lib is genesis
            Assert.Equal(Genesis.GetHash(), Synchronizer.CurrentLib.BlockHash);
            
            await Synchronizer.TryPushBlock(block3);
            
            Assert.Equal(block1.GetHash(), Synchronizer.CurrentLib.BlockHash);
        }

        [Fact]
        public async Task TryPushBlock_ReceiveBlockOnLowerFork_ShouldNotExecute()
        {
            GenesisChainSetup();

            Synchronizer.Init();

            IBlock forkRoot = SyncTestHelpers.BuildNext(Genesis); // Height 2
            
            IBlock blockForkA = SyncTestHelpers.BuildNext(forkRoot);  // Height 3
            IBlock blockForkA1 = SyncTestHelpers.BuildNext(blockForkA); // Height 4
            
            IBlock blockForkB = SyncTestHelpers.BuildNext(forkRoot); // Height 3 - should not exec

            await Synchronizer.TryPushBlock(forkRoot);
            await Synchronizer.TryPushBlock(blockForkA);
            await Synchronizer.TryPushBlock(blockForkA1);
            
            MonitorExecuteBlockCalls();
            await Synchronizer.TryPushBlock(blockForkB);
            
            Assert.Empty(ExecuteBlockCalls);
        }
    }
}