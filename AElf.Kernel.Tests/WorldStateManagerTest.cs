using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateManagerTest
    {
        private readonly IWorldStateManager _worldStateManager;

        private readonly Hash _genesisBlockHash = Hash.Zero;
        
        public WorldStateManagerTest(IWorldStateStore worldStateStore, IPointerCollection pointerCollection, IAccountContextService accountContextService, IChangesCollection changesCollection, IDataStore dataStore)
        {
            _worldStateManager = new WorldStateManager(worldStateStore, _genesisBlockHash, 
                accountContextService, pointerCollection, changesCollection, dataStore);
        }

        [Fact]
        public async Task DataTest()
        {
            var key = Hash.Generate();
            var data = Hash.Generate().Value.ToArray();
            await _worldStateManager.SetData(key, data);

            var getData = await _worldStateManager.GetData(key);
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        public async Task AccountDataProviderTest()
        {
            var chain = new Chain(Hash.Generate());
            var address = Hash.Generate();

            var accountDataProvider = _worldStateManager.GetAccountDataProvider(chain.Id, address);
            
            Assert.True(accountDataProvider.Context.Address == address);
            Assert.True(accountDataProvider.Context.ChainId == chain.Id);
            
            var dataProvider = accountDataProvider.GetDataProvider();
            var data = Hash.Generate().Value.ToArray();
            await dataProvider.SetAsync(data);
            var getData = await dataProvider.GetAsync();
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        public async Task WorldStateTest()
        {
            var chain = new Chain(Hash.Generate());
            var block = new Block(Hash.Generate());
            await _worldStateManager.SetWorldStateToCurrentState(chain.Id, block.GetHash());

            var genesisWorldState = await _worldStateManager.GetWorldStateAsync(chain.Id, _genesisBlockHash);
            var changes = await genesisWorldState.GetChangesAsync();
            
            Assert.True(changes.Count == 0);
        }
    }
}