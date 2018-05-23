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
    public class WorldStateTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public WorldStateTest(IChainStore chainStore, IWorldStateStore worldStateStore, 
            IChangesStore changesStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }
        
        [Fact]
        public async Task GetWorldStateTest()
        {
            var genesisBlockHash = Hash.Generate();
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var block0 = CreateBlock(genesisBlockHash);
            var block = CreateBlock(block0.GetHash());
            var chainManger = new ChainManager(_chainStore, _dataStore);

            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);

            await worldStateManager.SetWorldStateAsync(block0.GetHash());
            
            await chainManger.AddChainAsync(chain.Id, genesisBlockHash);
            await chainManger.AppendBlockToChainAsync(chain.Id, block0);

            await worldStateManager.SetWorldStateAsync(block.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block);
            
            var worldState = await worldStateManager.GetWorldStateAsync(block0.GetHash());
            
            Assert.NotNull(worldState);
        }

        [Fact]
        public async Task GetHistoryWorldStateRootTest()
        {
            var genesisBlockHash = Hash.Generate();
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var block1 = CreateBlock(genesisBlockHash);
            var block2 = CreateBlock(block1.GetHash());
            var chainManger = new ChainManager(_chainStore, _dataStore);
            await chainManger.AddChainAsync(chain.Id, genesisBlockHash);

            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            await worldStateManager.SetWorldStateAsync(genesisBlockHash);
            
            var key = new Hash("testkey".CalculateHash());

            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(key, data1);
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(key, data2);
            var data3= Hash.Generate().Value.ToArray();
            var subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(key, data3);
            var data4 = Hash.Generate().Value.ToArray();
            var subDataProvider4 = dataProvider.GetDataProvider("test4");
            await subDataProvider4.SetAsync(key, data4);
            
            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block1);

            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().Value.ToArray();
            subDataProvider1 = dataProvider.GetDataProvider("test1");
            await subDataProvider1.SetAsync(key, data5);
            var data6 = Hash.Generate().Value.ToArray();
            subDataProvider2 = dataProvider.GetDataProvider("test2");
            await subDataProvider2.SetAsync(key, data6);
            var data7= Hash.Generate().Value.ToArray();
            subDataProvider3 = dataProvider.GetDataProvider("test3");
            await subDataProvider3.SetAsync(key, data7);
            
            var changes1 = await worldStateManager.GetChangesAsync(genesisBlockHash);
            
            await chainManger.AppendBlockToChainAsync(chain.Id, block2);
            await worldStateManager.SetWorldStateAsync(block2.GetHash());

            //Test the continuity of changes (through two sequence world states).
            var getChanges1 = await worldStateManager.GetChangesAsync(genesisBlockHash);
            var changes2 = await worldStateManager.GetChangesAsync(block1.GetHash());
            Assert.True(changes1.Count == getChanges1.Count);
            Assert.True(changes1[0].After == getChanges1[0].After);
            Assert.True(changes1[3].After == getChanges1[3].After);
            Assert.True(changes1[0].After == changes2[0].GetLastHashBefore());
            Assert.True(changes1[1].After == changes2[1].GetLastHashBefore());
            Assert.True(changes1[2].After == changes2[2].GetLastHashBefore());

            //Test the equality of pointer transfered from path and get from world state.
            var path = new Path()
                .SetChainHash(chain.Id)
                .SetAccount(address)
                .SetDataProvider(subDataProvider1.GetHash())
                .SetDataKey(key);
            var pointerHash1 = path.SetBlockHash(genesisBlockHash).GetPointerHash();
            var pointerHash2 = path.SetBlockHash(block1.GetHash()).GetPointerHash();
            Assert.True(changes2[0].GetLastHashBefore() == pointerHash1);
            Assert.True(changes2[0].After == pointerHash2);

            //Test data equal or not equal from different world states.
            var getData1InHeight1 = await subDataProvider1.GetAsync(key, genesisBlockHash);
            var getData1InHeight2 = await subDataProvider1.GetAsync(key, block1.GetHash());
            var getData1 = await subDataProvider1.GetAsync(key);
            Assert.True(data1.SequenceEqual(getData1InHeight1));
            Assert.False(data1.SequenceEqual(getData1InHeight2));
            Assert.True(data5.SequenceEqual(getData1InHeight2));
            Assert.True(getData1InHeight2.SequenceEqual(getData1));

            Assert.True(changes2.Count == 3);
            
            var block3 = CreateBlock(block2.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block3);
            await worldStateManager.SetWorldStateAsync(block3.GetHash());

            var changes3 = await worldStateManager.GetChangesAsync();
            
            Assert.True(changes3.Count == 0);

            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data8 = Hash.Generate().Value.ToArray();
            var subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data8);
            
            var block4 = CreateBlock(block3.GetHash());
            await worldStateManager.SetWorldStateAsync(block4.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block4);

            var changes4 = await worldStateManager.GetChangesAsync(block3.GetHash());
            
            Assert.True(changes4.Count == 1);
            var getData8 = await subDataProvider5.GetAsync(key);
            Assert.True(data8.SequenceEqual(getData8));
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data9 = Hash.Generate().Value.ToArray();
            subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data9);
            
            await worldStateManager.RollbackCurrentChangesAsync();
            
            var getData9 = await subDataProvider5.GetAsync(key);
            Assert.False(data9.SequenceEqual(getData9));
            Assert.True(data8.SequenceEqual(getData9));
        }

        [Fact]
        public async Task RollbackCurrentChangesTest()
        {
            var genesisBlockHash = Hash.Generate();
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var block1 = CreateBlock(genesisBlockHash);
            var block2 = CreateBlock(block1.GetHash());
            var chainManger = new ChainManager(_chainStore, _dataStore);
            await chainManger.AddChainAsync(chain.Id, genesisBlockHash);
            
            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            await worldStateManager.SetWorldStateAsync(genesisBlockHash);
            
            var key1 = new Hash("testkey1".CalculateHash());
            var key2 = new Hash("testkey2".CalculateHash());

            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data1);
            await subDataProvider.SetAsync(key2, data2);
            
            await worldStateManager.SetWorldStateAsync(block1.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block1);
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data3 = Hash.Generate().Value.ToArray();
            var data4 = Hash.Generate().Value.ToArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data3);
            await subDataProvider.SetAsync(key2, data4);

            var getData3 = await subDataProvider.GetAsync(key1);
            Assert.True(data3.SequenceEqual(getData3));
            var getData4 = await subDataProvider.GetAsync(key2);
            Assert.True(data4.SequenceEqual(getData4));

            //Do the rollback
            await worldStateManager.RollbackCurrentChangesAsync();

            //Now the "key"'s value of subDataProvider rollback to previous data.
            var getData1 = await subDataProvider.GetAsync(key1);
            var getData2 = await subDataProvider.GetAsync(key2);
            Assert.False(data3.SequenceEqual(getData1));
            Assert.False(data4.SequenceEqual(getData2));
            Assert.True(data1.SequenceEqual(getData1));
            Assert.True(data2.SequenceEqual(getData2));
            
            //Set again
            await subDataProvider.SetAsync(key1, data3);
            await subDataProvider.SetAsync(key2, data4);
            
            Assert.True((await subDataProvider.GetAsync(key1)).SequenceEqual(data3));
            Assert.True((await subDataProvider.GetAsync(key2)).SequenceEqual(data4));

            await worldStateManager.SetWorldStateAsync(block2.GetHash());
            await chainManger.AppendBlockToChainAsync(chain.Id, block2);

            accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data5 = Hash.Generate().Value.ToArray();
            var data6 = Hash.Generate().Value.ToArray();
            subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data6);
            await subDataProvider.SetAsync(key1, data5);

            var getData5 = await subDataProvider.GetAsync(key1);
            Assert.True(getData5.SequenceEqual(data5));

            await worldStateManager.RollbackCurrentChangesAsync();

            getData3 = await subDataProvider.GetAsync(key1);
            Assert.True(getData3.SequenceEqual(data3));
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