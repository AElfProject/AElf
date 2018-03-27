using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IBlockStore _blockStore;

        private readonly IChainBlockRelationStore _chainBlockRelationStore;

        private readonly IChainStore _chainStore;
        
        public StoragesTest(IBlockStore blockStore, IChainBlockRelationStore chainBlockRelationStore, IChainStore chainStore)
        {
            _blockStore = blockStore;
            _chainBlockRelationStore = chainBlockRelationStore;
            _chainStore = chainStore;
        }
        
        [Fact]
        public void BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            _blockStore.Insert(block);

            var hash = block.GetHash();
            var getBlock = _blockStore.GetAsync(hash).Result;
            
            Assert.True(block == getBlock);
        }

        [Fact]
        public async void ChainBlockRelationStoreTest()
        {
            var chain = new Chain();

            //Prepare
            var preBlockHash = Hash.Generate();
            var preBlock = new Block(preBlockHash);
            preBlock.AddTransaction(Hash.Generate());
            chain.UpdateCurrentBlock(preBlock);
            
            //Add a new block
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());

            var chainManager = new ChainManager(_chainBlockRelationStore, _chainStore);
            await chainManager.AppendBlockToChainAsync(chain, block);

            var blockHash = block.GetHash();
            var getBlockHash = _chainBlockRelationStore.GetAsync(chain.Id, chain.CurrentBlockHeight).Result;
            
            Assert.True(getBlockHash == blockHash);
        }
    }
}