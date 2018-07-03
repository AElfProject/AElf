using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class StoragesTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;
        private readonly ChainManager _chainManager;

        public StoragesTest(IWorldStateStore worldStateStore, IChangesStore changesStore, 
            IDataStore dataStore, BlockTest blockTest, ChainManager chainManager)
        {
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
            _blockTest = blockTest;
            _chainManager = chainManager;
        }
        
        [Fact]
        public async Task OneBlockDataTest()
        {
            //Create a chain with one block.
            var chain = await _blockTest.CreateChain();
            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await _chainManager.AppendBlockToChainAsync(block);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);
            await worldStateManager.SetWorldStateAsync(chain.GenesisBlockHash);
            var accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            
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
            //Create a chian and two blocks.
            var chain = await _blockTest.CreateChain();

            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            
            await _chainManager.AppendBlockToChainAsync(block1);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);
            var accountDataProvider = await worldStateManager.GetAccountDataProvider(address);

            await worldStateManager.SetWorldStateAsync(chain.GenesisBlockHash);

            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider("test");
            var data = new byte[] {1, 2, 3, 4};
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data);
            
            var getDataFromHeight1 = await subDataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getDataFromHeight1));

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            
            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            await _chainManager.AppendBlockToChainAsync(block2);
            
            accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider("test");
            
            var getDataFromHeight2 = await subDataProvider.GetAsync(key);

            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var data2 = new byte[] {1, 2, 3, 4, 5};
            await subDataProvider.SetAsync(key, data2);
            getDataFromHeight2 = await subDataProvider.GetAsync(key);
            
            Assert.False(getDataFromHeight1.SequenceEqual(getDataFromHeight2));

            var getDataFromHeight1ByBlockHash = await subDataProvider.GetAsync(key, chain.GenesisBlockHash);
            
            Assert.True(getDataFromHeight1.SequenceEqual(getDataFromHeight1ByBlockHash));
        }

        [Fact]
        public async Task MultiBlockDataTest()
        {
            const string str = "test";
            
            //Create a chian and several blocks.
            var chain = await _blockTest.CreateChain();

            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);

            //Add first block.
            await _chainManager.AppendBlockToChainAsync(block1);

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateManager = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);
            var accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            
            await worldStateManager.SetWorldStateAsync(chain.GenesisBlockHash);

            //Set data to one sub DataProvider("test").
            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider(str);
            var data1 = Hash.Generate().GetBytes();
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data1);

            //Set WorldState and add a new block.
            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);

            await _chainManager.AppendBlockToChainAsync(block2);

            //Must refresh the DataProviders before set new data.
            accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            //Change the data.
            var data2 = Hash.Generate().GetBytes();
            await subDataProvider.SetAsync(key, data2);
            Assert.False(data1.SequenceEqual(data2));

            //Check the ability to get data of previous WorldState.
            var getData1 = await subDataProvider.GetAsync(key, chain.GenesisBlockHash);
            Assert.True(data1.SequenceEqual(getData1));
            //And the ability to get data of current WorldState.(not equal to previous data)
            var getData2 = await subDataProvider.GetAsync(key);
            Assert.False(data1.SequenceEqual(getData2));
            
            //Now set WorldState again and add a third block.
            await worldStateManager.SetWorldStateAsync(block2.GetHash());
            
            var block3 = CreateBlock(block2.GetHash(), chain.Id, 3);

            await _chainManager.AppendBlockToChainAsync(block3);
            
            accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            var data3 = Hash.Generate().GetBytes();
            await subDataProvider.SetAsync(key, data3);

            //See the ability to get data of first WorldState.
            getData1 = await subDataProvider.GetAsync(key, chain.GenesisBlockHash);
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Block CreateBlock(Hash preBlockHash, Hash chainId, ulong index)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Zero, null);
            
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Index = index;
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();
            
            return block;
        }
    }
}