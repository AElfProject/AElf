using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class BlockTest
    {
        private readonly IBlockManager _blockManager;
        private readonly ChainTest _chainTest;
        private readonly IChainCreationService _chainCreationService;
        private readonly IChainManager _chainManager;

        public BlockTest(IBlockManager blockManager, ChainTest chainTest, IChainCreationService chainCreationService, IChainManager chainManager)
        {
            _blockManager = blockManager;
            //_smartContractZero = smartContractZero;
            _chainTest = chainTest;
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

        [Fact]
        public async Task GetNextBlockTest()
        {
            var chain = await CreateChain();
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await _chainManager.AppendBlockToChainAsync(block1);
            await _blockManager.AddBlockAsync(block1);
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await _chainManager.AppendBlockToChainAsync(block2);
            await _blockManager.AddBlockAsync(block2);

            var blockOfHeight2 = await _blockManager.GetNextBlockOf(chain.Id, block1.GetHash());
            
            Assert.Equal(block2, blockOfHeight2);
        }

        [Fact]
        public async Task GetBlockByHeightTest()
        {
            var chain = await CreateChain();
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await _chainManager.AppendBlockToChainAsync(block1);
            await _blockManager.AddBlockAsync(block1);
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await _chainManager.AppendBlockToChainAsync(block2);
            await _blockManager.AddBlockAsync(block2);

            var blockOfHeight1 = await _blockManager.GetBlockByHeight(chain.Id, 1);
            Assert.Equal(block1, blockOfHeight1);

            var blockOfHeight2 = await _blockManager.GetBlockByHeight(chain.Id, 2);
            Assert.Equal(block2, blockOfHeight2);

        }

        public async Task<IChain> CreateChain()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chainId = Hash.Generate();
            return await _chainCreationService.CreateNewChainAsync(chainId, reg);
        }
     
       [Fact]
        public void GenesisBlockBuilderTest()
        {
            var builder = new GenesisBlockBuilder().Build(Hash.Generate());
            var genesisBlock = builder.Block;
            //var txs = builder.Txs;
            Assert.NotNull(genesisBlock);
            Assert.Equal(genesisBlock.Header.PreviousBlockHash, Hash.Genesis);
            //Assert.NotNull(txs);
        }

        [Fact]
        public async Task BlockManagerTest()
        {
            var chain = await CreateChain();

            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await _blockManager.AddBlockAsync(block);
            var b = await _blockManager.GetBlockAsync(block.GetHash());
            Assert.Equal(b, block);
        }
        
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Genesis, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Index = index;
            block.Header.MerkleTreeRootOfWorldState = Hash.Default;

            block.Body.BlockHeader = block.Header.GetHash();

            
            return block;
        }
        
    }
}