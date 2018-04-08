using System.Threading.Tasks;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IBlockHeaderStore _blockStore;

        private readonly IChainBlockRelationStore _chainBlockRelationStore;

        private readonly IChainStore _chainStore;
        
        public StoragesTest(IBlockHeaderStore blockStore, IChainBlockRelationStore chainBlockRelationStore, IChainStore chainStore)
        {
            _blockStore = blockStore;
            _chainBlockRelationStore = chainBlockRelationStore;
            _chainStore = chainStore;
        }
        
        [Fact]
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            await _blockStore.InsertAsync(block.Header);

            var hash = block.GetHash();
            var getBlock = await _blockStore.GetAsync(hash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
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