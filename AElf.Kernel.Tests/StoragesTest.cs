using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
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
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public StoragesTest(IChainStore chainStore, IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            IWorldStateStore worldStateStore, IChangesStore changesStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        [Fact]
        public async Task ChainStoreTest()
        {
            var chainId = Hash.Generate();
            var genesisBlockHash = Hash.Generate();
            var chain = new Chain(chainId, genesisBlockHash);
            var chainManager = new ChainManager(_chainStore, _dataStore);
            
            await chainManager.AddChainAsync(chain.Id, genesisBlockHash);

            var block = CreateBlock(genesisBlockHash);
            
            await chainManager.AppendBlockToChainAsync(chain.Id, block);

            var getChain = await chainManager.GetChainAsync(chainId);
            
            Assert.True(chainId == chain.Id);
            Assert.True(chain.Id == getChain.Id);
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
        public async Task OneBlockDataTest()
        {
            var genesisBlockHash = Hash.Generate();
            //Create a chain with one block.
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var chainManager = new ChainManager(_chainStore, _dataStore);
            var block = CreateBlock(genesisBlockHash);
            await chainManager.AddChainAsync(chain.Id, genesisBlockHash);
            await chainManager.AppendBlockToChainAsync(chain.Id, block);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            await worldStateManager.SetWorldStateAsync(genesisBlockHash);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            
            //Get the DataProvider of the AccountDataProvider.
            var dataProvider = accountDataProvider.GetDataProvider();

            //Set data to the DataProvider and get it.
            var data = new byte[] {1, 1, 1, 1};
            var key = new Hash("testkey".CalculateHash());
            await dataProvider.SetAsync(key, data);
            var getData = await dataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getData));

            //Get a sub-DataProvider from aforementioned DataProvider.
            var subDataProvider = dataProvider.GetDataProvider("testdp");

            //Same as before.
            var data2 = new byte[] {1, 2, 3, 4};

            await subDataProvider.SetAsync(key, data2);
            var getData2 = await subDataProvider.GetAsync(key);
            
            Assert.True(data2.SequenceEqual(getData2));

            var data3 = new byte[] {4, 3, 2, 1};

            await subDataProvider.SetAsync(key, data3);
            var getData3 = await subDataProvider.GetAsync(key);
            
            Assert.True(data3.SequenceEqual(getData3));
        }

        [Fact]
        public async Task TwoBlockDataTest()
        {
            var genesisBlockHash = Hash.Generate();
            //Create a chian and two blocks.
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var chainManager = new ChainManager(_chainStore, _dataStore);

            var block1 = CreateBlock(genesisBlockHash);
            var block2 = CreateBlock(block1.GetHash());
            
            await chainManager.AddChainAsync(chain.Id, genesisBlockHash);
            await chainManager.AppendBlockToChainAsync(chain.Id, block1);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);

            await worldStateManager.SetWorldStateAsync(genesisBlockHash);

            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider("test");
            var data = new byte[] {1, 2, 3, 4};
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data);
            
            var getDataFromHeight1 = await subDataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getDataFromHeight1));

            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            await chainManager.AppendBlockToChainAsync(chain.Id, block2);
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider("test");
            
            var getDataFromHeight2 = await subDataProvider.GetAsync(key);

            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var data2 = new byte[] {1, 2, 3, 4, 5};
            await subDataProvider.SetAsync(key, data2);
            getDataFromHeight2 = await subDataProvider.GetAsync(key);
            
            Assert.False(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var getDataFromHeight1ByBlockHash = await subDataProvider.GetAsync(key, genesisBlockHash);
            
            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight1ByBlockHash));
        }

        [Fact]
        public async Task MultiBlockDataTest()
        {
            const string str = "test";
            
            var genesisBlockHash = Hash.Generate();

            //Create a chian and several blocks.
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var chainManager = new ChainManager(_chainStore, _dataStore);

            var block1 = CreateBlock(genesisBlockHash);

            //Add first block.
            await chainManager.AddChainAsync(chain.Id, genesisBlockHash);
            await chainManager.AppendBlockToChainAsync(chain.Id, block1);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            
            await worldStateManager.SetWorldStateAsync(genesisBlockHash);

            //Set data to one sub DataProvider("test").
            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider(str);
            var data1 = Hash.Generate().Value.ToByteArray();
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data1);

            //Set WorldState and add a new block.
            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            
            var block2 = CreateBlock(block1.GetHash());

            await chainManager.AppendBlockToChainAsync(chain.Id, block2);

            //Must refresh the DataProviders before set new data.
            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            //Change the data.
            var data2 = Hash.Generate().Value.ToByteArray();
            await subDataProvider.SetAsync(key, data2);
            Assert.False(data1.SequenceEqual(data2));

            //Check the ability to get data of previous WorldState.
            var getData1 = await subDataProvider.GetAsync(key, genesisBlockHash);
            Assert.True(data1.SequenceEqual(getData1));
            //And the ability to get data of current WorldState.(not equal to previous data)
            var getData2 = await subDataProvider.GetAsync(key);
            Assert.False(data1.SequenceEqual(getData2));
            
            //Now set WorldState again and add a third block.
            await worldStateManager.SetWorldStateAsync(block2.GetHash());
            
            var block3 = CreateBlock(block2.GetHash());

            await chainManager.AppendBlockToChainAsync(chain.Id, block3);
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            var data3 = Hash.Generate().Value.ToByteArray();
            await subDataProvider.SetAsync(key, data3);

            //See the ability to get data of first WorldState.
            getData1 = await subDataProvider.GetAsync(key, genesisBlockHash);
            Assert.True(data1.SequenceEqual(getData1));
            //And second WorldState.
            getData2 = await subDataProvider.GetAsync(key, block1.GetHash());
            Assert.True(data2.SequenceEqual(getData2));
            //And the ability to get data of current WorldState.(not equal to previous data)
            var getData3 = await subDataProvider.GetAsync(key);
            Assert.False(data1.SequenceEqual(getData3));
            Assert.False(data2.SequenceEqual(getData3));
            Assert.True(data3.SequenceEqual(getData3));
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