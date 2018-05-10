using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
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
            var builder = new GenesisBlockBuilder().Build(_smartContractZero.GetType());
            var genesisBlock = builder.Block;

            await _blockManager.AddBlockAsync(genesisBlock);
            var blockHeader = await _blockManager.GetBlockHeaderAsync(genesisBlock.GetHash());

            var block = new Block(genesisBlock.GetHash());

            var chain = await _chainTest.CreateChainTest();
            await _chainTest.AppendBlockTest(chain, block);
            Assert.Equal(blockHeader, genesisBlock.Header);
        }
        
        
    }
}