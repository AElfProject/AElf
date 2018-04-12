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
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;

        private readonly IWorldStateStore _worldStateStore;

        private readonly IPointerStore _pointerStore;
        
        private readonly IChainStore _chainStore;

        public StoragesTest(IChainStore chainStore, 
            IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            IWorldStateStore worldStateStore, IPointerStore pointerStore)
        {
            _chainStore = chainStore;
            
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
        }

        [Fact]
        public async Task ChainStoreTest()
        {
            var chain = new Chain();
            var chainManager = new ChainManager(_chainStore);
            
            await chainManager.AddChainAsync(chain.Id);
        }

        [Fact]
        public async Task BlockStoreTest()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();

            var blockHeaderStore = _blockHeaderStore;
            var blockBodyStore = _blockBodyStore;
            
            await blockHeaderStore.InsertAsync(block.Header);
            await blockBodyStore.InsertAsync(block.Header.MerkleTreeRootOfTransactions, block.Body);

            //block hash = block header hash
            var blockHeaderHash = block.Header.GetHash();
            var blockHash = block.GetHash();
            Assert.True(blockHash == blockHeaderHash);
            
            var getBlock = await blockHeaderStore.GetAsync(blockHeaderHash);
            
            Assert.True(block.Header.GetHash() == getBlock.GetHash());
            
            //block body hash = transactions merkle tree root hash
            var blockBodyHash = block.Header.MerkleTreeRootOfTransactions;
            var getBlockBody = await blockBodyStore.GetAsync(blockBodyHash);
            
            Assert.True(Equals(block.Body, getBlockBody));
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
            
            Assert.True(data.SequenceEqual(getData));

            //Get a sub-DataProvider from aforementioned DataProvider.
            var subDataProvider = dataProvider.GetDataProvider("test");

            //Same as before.
            var data2 = new byte[] {1, 2, 3, 4};

            await subDataProvider.SetAsync(data2);
            var getData2 = await subDataProvider.GetAsync(preBlockHash);
            
            Assert.True(data2.SequenceEqual(getData2));

        }
    }
}