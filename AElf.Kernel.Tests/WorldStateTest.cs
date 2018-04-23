using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerStore _pointerStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public WorldStateTest(IChainStore chainStore, IWorldStateStore worldStateStore, 
            IPointerStore pointerStore, IChangesStore changesStore, IDataStore dataStore)
        {
            _chainStore = chainStore;
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }
        
        [Fact]
        public async Task GetWorldStateTest()
        {
            var chain = new Chain(Hash.Generate());
            var block0 = CreateBlock();
            var block = CreateBlock();
            var chainManger = new ChainManager(_chainStore);

            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, block0.GetHash(), 
                accountContextService, _pointerStore, _changesStore, _dataStore);

            await chainManger.AddChainAsync(chain.Id);
            await chainManger.AppendBlockToChainAsync(chain, block0);

            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block);

            var worldState = await worldStateManager.GetWorldStateAsync(chain.Id, block0.GetHash());
            
            Assert.NotNull(worldState);
            Assert.NotNull(worldState.GetWorldStateMerkleTreeRootAsync());
        }

        [Fact]
        public async Task GetHistoryWorldStateRootTest()
        {
            var chain = new Chain(Hash.Generate());
            var block0 = CreateBlock();
            var block1 = CreateBlock();
            var chainManger = new ChainManager(_chainStore);
            await chainManger.AddChainAsync(chain.Id);
            await chainManger.AppendBlockToChainAsync(chain, block0);

            var address = Hash.Generate();
            var accountContextService = new AccountContextService();
            var worldStateManager = new WorldStateManager(_worldStateStore, block0.GetHash(), 
                accountContextService, _pointerStore, _changesStore, _dataStore);
            
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block0.GetHash());

            var key = new Hash("testkey".CalculateHash());
            
            var accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
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
            
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block1.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block1);

            var worldState1 = await worldStateManager.GetWorldStateAsync(chain.Id, block0.GetHash());
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
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
            
            var block2 = CreateBlock();
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block2.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block2);

            var worldState1Again = await worldStateManager.GetWorldStateAsync(chain.Id, block0.GetHash());
            var worldState2 = await worldStateManager.GetWorldStateAsync(chain.Id, block1.GetHash());

            var changes1 = await worldState1.GetChangesAsync();
            var getChanges1 = await worldState1Again.GetChangesAsync();
            var changes2 = await worldState2.GetChangesAsync();
            Assert.True(changes1.Count == getChanges1.Count);
            Assert.True(changes1[0].After == getChanges1[0].After);
            Assert.True(changes1[3].After == getChanges1[3].After);
            Assert.True(changes1[0].After == changes2[0].Before);
            Assert.True(changes1[1].After == changes2[1].Before);
            Assert.True(changes1[2].After == changes2[2].Before);

            var getData1InHeight1 = await subDataProvider1.GetAsync(block0.GetHash());
            var getData1InHeight2 = await subDataProvider1.GetAsync(block1.GetHash());
            var getData1 = await subDataProvider1.GetAsync(key);
            Assert.True(data1.SequenceEqual(getData1InHeight1));
            Assert.False(data1.SequenceEqual(getData1InHeight2));
            Assert.True(data5.SequenceEqual(getData1InHeight2));
            Assert.True(getData1InHeight2.SequenceEqual(getData1));

            Assert.True(changes2.Count == 3);
            
            var block3 = CreateBlock();
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block3.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block3);

            var worldState3 = await worldStateManager.GetWorldStateAsync(chain.Id, block2.GetHash());
            var changes3 = await worldState3.GetChangesAsync();
            
            Assert.True(changes3.Count == 0);
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data8 = Hash.Generate().Value.ToArray();
            var subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data8);
            
            var block4 = CreateBlock();
            await worldStateManager.SetWorldStateToCurrentState(chain.Id, block4.GetHash());
            await chainManger.AppendBlockToChainAsync(chain, block4);

            var worldState4 = await worldStateManager.GetWorldStateAsync(chain.Id, block3.GetHash());
            var changes4 = await worldState4.GetChangesAsync();
            
            Assert.True(changes4.Count == 1);
            var getData8 = await subDataProvider5.GetAsync(key);
            Assert.True(data8.SequenceEqual(getData8));
            
            accountDataProvider = worldStateManager.GetAccountDataProvider(chain.Id, address);
            dataProvider = accountDataProvider.GetDataProvider();
            var data9 = Hash.Generate().Value.ToArray();
            subDataProvider5 = dataProvider.GetDataProvider("test5");
            await subDataProvider5.SetAsync(key, data9);
            
            await worldStateManager.RollbackDataToPreviousWorldState();
            
            var getData9 = await subDataProvider5.GetAsync(key);
            Assert.False(data9.SequenceEqual(getData9));
            Assert.True(data8.SequenceEqual(getData9));
        }
        
        private Block CreateBlock()
        {
            var block = new Block(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.AddTransaction(Hash.Generate());
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }
    }
}