using System.Collections.Generic;
using System.Linq;
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
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            
            var blockHeaderStore = new BlockHeaderStore(new KeyValueDatabase());
            
            await blockHeaderStore.InsertAsync(block.Header);

            var hash = block.GetHash();
            var getBlock = await blockHeaderStore.GetAsync(hash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
        }
        
        [Fact]
        public async Task OneBlockDataProviderTest()
        {
            #region Prepare data
            //Create a chain with one block.
            var chain = new Chain();
            var preBlockHash = Hash.Generate();
            var preBlock = new Block(preBlockHash);
            preBlock.AddTransaction(Hash.Generate());
            chain.UpdateCurrentBlock(preBlock);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, preBlockHash, 
                accountContextService, _pointerStore);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            #endregion
            
            //Get the DataProvider of the AccountDataProvider.
            var dataProvider = accountDataProvider.GetDataProvider();

            //Set data to the DataProvider and get it.
            var data = new byte[] {1, 1, 1, 1};
            await dataProvider.SetAsync(data);
            var getData = await dataProvider.GetAsync(preBlockHash);
            
            Assert.True(data == getData);

            //Get a sub-DataProvider from aforementioned DataProvider.
            var subDataProvider = dataProvider.GetDataProvider("test");

            //Same as before.
            var data2 = new byte[] {1, 2, 3, 4};
            await subDataProvider.SetAsync(data2);
            var getData2 = await subDataProvider.GetAsync(preBlockHash);
            
            Assert.True(data2 == getData2);
        }
    }
}