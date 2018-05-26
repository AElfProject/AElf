using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using Google.Protobuf.Collections;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class BlockTest
    {
        private readonly IBlockManager _blockManager;
        private readonly ISmartContractZero _smartContractZero;
        private readonly ChainTest _chainTest;

        public BlockTest(IBlockManager blockManager, ISmartContractZero smartContractZero, ChainTest chainTest)
        {
            _blockManager = blockManager;
            _smartContractZero = smartContractZero;
            _chainTest = chainTest;
        }
     
       [Fact]
        public void GenesisBlockBuilderTest()
        {
            var builder = new GenesisBlockBuilder().Build(_smartContractZero.GetType());
            var genesisBlock = builder.Block;
            var txs = builder.Txs;
            Assert.NotNull(genesisBlock);
            Assert.Equal(genesisBlock.Header.PreviousHash, Hash.Zero);
            Assert.NotNull(txs);
        }

        [Fact]
        public async Task BlockManagerTest()
        {
            var chain = await _chainTest.CreateChainTest();

            var block = CreateBlock(chain.GenesisBlockHash);
            await _blockManager.AddBlockAsync(block);
            var b = await _blockManager.GetBlockAsync(block.GetHash());
            Assert.Equal(b, block);
        }
        
        private Block CreateBlock(Hash preBlockHash = null)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousHash = preBlockHash;
            return block;
        }
        
    }
}