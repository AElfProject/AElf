using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IBlockHeaderStore _blockStore;

        private readonly IWorldStateStore _worldStateStore;

        private readonly IPointerStore _pointerStore;
        
        private readonly IChainStore _chainStore;

        public StoragesTest(IBlockHeaderStore blockStore, IChainStore chainStore, 
            IWorldStateStore worldStateStore, IPointerStore pointerStore)
        {
            _blockStore = blockStore;
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
        }

        [Fact]
        public void Test()
        {
            Assert.True(true);
        }
        
        [Fact]
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            var blockStore = new BlockHeaderStore(new KeyValueDatabase());
            
            await blockStore.InsertAsync(block.Header);

            var hash = block.GetHash();
            var getBlock = await blockStore.GetAsync(hash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
        }

        [Fact]
        public async void AccountDataChangeTest()
        {
            #region Generate a chain with one block
            
            var chain = new Chain();
            
            var preBlockHash = Hash.Generate();
            var preBlock = new Block(preBlockHash);
            preBlock.AddTransaction(Hash.Generate());
            chain.UpdateCurrentBlock(preBlock);
            
            #endregion

            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, preBlockHash, 
                accountContextService, _pointerStore);
            var worldState = worldStateManager.GetWorldStateAsync(chain.Id);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);

            var dataProvider = accountDataProvider.GetDataProvider();

            var key = Hash.Generate();
            
            var data = new byte[] {1, 1, 1, 1};
            await dataProvider.SetAsync(preBlockHash, data);
            var getData = await dataProvider.GetAsync(preBlockHash);
            
            Assert.True(data == getData);
            
        }
    }
}