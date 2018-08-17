using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Kernel.TxMemPool;
using Google.Protobuf.WellKnownTypes;
using NLog;
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
        private readonly IChainService _chainService;
        private readonly ILogger _logger;
        private readonly ITxPoolService _txPoolService;
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly ITransactionStore _transactionStore;

        public StoragesTest(IWorldStateStore worldStateStore, IChangesStore changesStore, 
            IDataStore dataStore, BlockTest blockTest, IChainService chainService,
            ILogger logger, ITxPoolService txPoolService, IBlockHeaderStore blockHeaderStore, ITransactionStore transactionStore, IBlockBodyStore blockBodyStore)
        {
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
            _blockTest = blockTest;
            _chainService = chainService;
            _logger = logger;
            _txPoolService = txPoolService;
            _blockHeaderStore = blockHeaderStore;
            _transactionStore = transactionStore;
            _blockBodyStore = blockBodyStore;
        }
        
        [Fact]
        public async Task OneBlockDataTest()
        {
            //Create a chain with one block.
            var chain = await _blockTest.CreateChain();
            var block = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            var blockchain = _chainService.GetBlockChain(chain.Id);
            await blockchain.AddBlocksAsync(new List<IBlock>() { block });

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore, _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();
            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            
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
            
            var blockchain = _chainService.GetBlockChain(chain.Id);
            await blockchain.AddBlocksAsync(new List<IBlock>() { block1 });

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore, _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();

            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);

            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);

            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider("test");
            var data = new byte[] {1, 2, 3, 4};
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data);
            
            var getDataFromHeight1 = await subDataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getDataFromHeight1));

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            await blockchain.AddBlocksAsync(new List<IBlock>() { block2 });
            
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
            var blockchain = _chainService.GetBlockChain(chain.Id);
            await blockchain.AddBlocksAsync(new List<IBlock>() { block1 });

            //Create an Account as well as an AccountDataProvider.
            var address = Hash.Generate();
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore, _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();

            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            
            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);

            //Set data to one sub DataProvider("test").
            var dataProvider = accountDataProvider.GetDataProvider();
            var subDataProvider = dataProvider.GetDataProvider(str);
            var data1 = Hash.Generate().GetHashBytes();
            var key = new Hash("testkey".CalculateHash());
            await subDataProvider.SetAsync(key, data1);

            //Set WorldState and add a new block.
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);

            await blockchain.AddBlocksAsync(new List<IBlock>() { block2 });

            //Must refresh the DataProviders before set new data.
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            //Change the data.
            var data2 = Hash.Generate().GetHashBytes();
            await subDataProvider.SetAsync(key, data2);
            Assert.False(data1.SequenceEqual(data2));

            //Check the ability to get data of previous WorldState.
            var getData1 = await subDataProvider.GetAsync(key, chain.GenesisBlockHash);
            Assert.True(data1.SequenceEqual(getData1));
            //And the ability to get data of current WorldState.(not equal to previous data)
            var getData2 = await subDataProvider.GetAsync(key);
            Assert.False(data1.SequenceEqual(getData2));
            
            //Now set WorldState again and add a third block.
            await worldStateDictator.SetWorldStateAsync(block2.GetHash());
            
            var block3 = CreateBlock(block2.GetHash(), chain.Id, 3);

            await blockchain.AddBlocksAsync(new List<IBlock>() { block3 });
            
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            subDataProvider = dataProvider.GetDataProvider(str);
            var data3 = Hash.Generate().GetHashBytes();
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