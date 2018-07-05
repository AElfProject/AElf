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
    public class WorldStateTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;

        public WorldStateTest(IChainStore chainStore, IWorldStateStore worldStateStore, 
            IChangesStore changesStore, IDataStore dataStore, BlockTest blockTest)
        {
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
            _blockTest = blockTest;
        }
        
        [Fact]
        public async Task GetWorldStateTest()
        {
            // Data preparation
            var chain = await _blockTest.CreateChain();
            var worldStateDirector = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);
            var chainManger = new ChainManager(_chainStore, _dataStore, worldStateDirector);
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            
            //because the hash value of block1 will be changed in appending operation, 
            //so reverse this two operations.
            await chainManger.AppendBlockToChainAsync(block1);
            await worldStateDirector.SetWorldStateAsync(block1.GetHash());

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await chainManger.AppendBlockToChainAsync(block2);
            await worldStateDirector.SetWorldStateAsync(block2.GetHash());

            var worldState = await worldStateDirector.GetWorldStateAsync(block1.GetHash());
            
            Assert.NotNull(worldState);
        }

        [Fact]
        public async Task GetHistoryWorldStateRootTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);

            var chainManger = new ChainManager(_chainStore, _dataStore, worldStateDictator);

            var key = new Hash("testkey".CalculateHash());
            
            var address = Hash.Generate();
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            await chainManger.AppendBlockToChainAsync(block1);

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
            
            var changes1 = await worldStateDictator.GetChangesAsync(chain.GenesisBlockHash);
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash()); 
            await chainManger.AppendBlockToChainAsync(block2);

            //Test the continuity of changes (through two sequence world states).
            var getChanges1 = await worldStateDictator.GetChangesAsync(chain.GenesisBlockHash);
            var changes2 = await worldStateDictator.GetChangesAsync(block1.GetHash());
            Assert.True(changes1.Count == getChanges1.Count);
            Assert.True(changes1[0].After == getChanges1[0].After);
            Assert.True(changes1[3].After == getChanges1[3].After);
            Assert.True(changes1.Select(c => c.After).
                            Intersect(changes2.Select(c => c.Befores.FirstOrDefault())).Count() == 3);

            //Test the equality of pointer transfered from path and get from world state.
            var path = new Path()
                .SetChainHash(chain.Id)
                .SetAccount(address)
                .SetDataProvider(subDataProvider1.GetHash())
                .SetDataKey(key);
            var pointerHash1 = path.SetBlockHash(chain.GenesisBlockHash).GetPointerHash();
            var pointerHash2 = path.SetBlockHash(block1.GetHash()).GetPointerHash();
            Assert.True(changes2[1].GetLastHashBefore() == pointerHash1);
            Assert.True(changes2[1].After == pointerHash2);

            //Test data equal or not equal from different world states.
            var getData1InHeight1 = await subDataProvider1.GetAsync(key, chain.GenesisBlockHash);
            var getData1InHeight2 = await subDataProvider1.GetAsync(key, block1.GetHash());
            var getData1 = await subDataProvider1.GetAsync(key);
            Assert.True(data1.SequenceEqual(getData1InHeight1));
            Assert.False(data1.SequenceEqual(getData1InHeight2));
            Assert.True(data5.SequenceEqual(getData1InHeight2));
            Assert.True(getData1InHeight2.SequenceEqual(getData1));

            var block3 = CreateBlock(block2.GetHash(), chain.Id, 3);
            await chainManger.AppendBlockToChainAsync(block3);
            await worldStateDictator.SetWorldStateAsync(block3.GetHash());

            var changes3 = await worldStateDictator.GetChangesAsync();
            
            Assert.True(changes3.Count == 0);

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data8 = Hash.Generate().Value.ToArray();
            var subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data8);
            
            var block4 = CreateBlock(block3.GetHash(), chain.Id, 4);
            await chainManger.AppendBlockToChainAsync(block4);
            await worldStateDictator.SetWorldStateAsync(block4.GetHash());

            var changes4 = await worldStateDictator.GetChangesAsync(block3.GetHash());
            
            Assert.True(changes4.Count == 2);
            var getData8 = await subDataProvider5.GetAsync(key);
            Assert.True(data8.SequenceEqual(getData8));
            
            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data9 = Hash.Generate().Value.ToArray();
            subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data9);
            
            await worldStateDictator.RollbackCurrentChangesAsync();
            
            var getData9 = await subDataProvider5.GetAsync(key);
            Assert.False(data9.SequenceEqual(getData9));
            Assert.True(data8.SequenceEqual(getData9));
        }

        [Fact]
        public async Task RollbackCurrentChangesTest()
        {
            var chain = await _blockTest.CreateChain();
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            
            var worldStateManager = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);
            var chainManger = new ChainManager(_chainStore, _dataStore, worldStateManager);
            
            var address = Hash.Generate();
            
            var key1 = new Hash("testkey1".CalculateHash());
            var key2 = new Hash("testkey2".CalculateHash());

            var accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var data2 = Hash.Generate().Value.ToArray();
            var subDataProvider = dataProvider.GetDataProvider("test");
            await subDataProvider.SetAsync(key1, data1);
            await subDataProvider.SetAsync(key2, data2);
            
            await chainManger.AppendBlockToChainAsync(block1);
            await worldStateManager.SetWorldStateAsync(block1.GetHash());

            accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
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

            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await chainManger.AppendBlockToChainAsync(block2);
            await worldStateManager.SetWorldStateAsync(block2.GetHash());

            accountDataProvider = await worldStateManager.GetAccountDataProvider(address);
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

        [Fact]
        public async Task RollbackToSpecificHeightTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore).SetChainId(chain.Id);

            var chainManger = new ChainManager(_chainStore, _dataStore, worldStateDictator);

            var key = new Hash("testkey".CalculateHash());
            
            var address = Hash.Generate();
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
            
            var block1 = CreateBlock(chain.GenesisBlockHash, chain.Id, 1);
            var foo = block1.GetHash();
            await worldStateDictator.SetWorldStateAsync(block1.GetHash());
            await chainManger.AppendBlockToChainAsync(block1);

            accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
            
            var block2 = CreateBlock(block1.GetHash(), chain.Id, 2);
            await worldStateDictator.SetWorldStateAsync(block2.GetHash()); 
            await chainManger.AppendBlockToChainAsync(block2);
            
            //Check value from height 2
            Assert.Equal(data5, await subDataProvider1.GetAsync(key));
            
            //Do rollback
            await worldStateDictator.RollbackToSpecificHeight(1);
            
            //Check result of rollback
            Assert.Equal(data1, await subDataProvider1.GetAsync(key));
        }

        /// <summary>
        /// the hash of block created by this method will be changed when appending to a chain.
        /// (basically change the block header's Index value)
        /// </summary>
        /// <param name="preBlockHash"></param>
        /// <param name="chainId"></param>
        /// <param name="index"></param>
        /// <returns></returns>
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
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Index = index;
            block.Header.MerkleTreeRootOfWorldState = Hash.Generate();

            return block;
        }
    }
}