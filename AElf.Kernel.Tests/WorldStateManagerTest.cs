using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateManagerTest
    {
        private readonly IWorldStateManager _worldStateManager;

        private readonly BlockTest _blockTest;

        public WorldStateManagerTest(IWorldStateStore worldStateStore, IChangesStore changesStore, 
            IDataStore dataStore, BlockTest blockTest)
        {
            _worldStateManager = new WorldStateManager(worldStateStore, changesStore, dataStore);
            _blockTest = blockTest;

        }

        [Fact]
        public async Task DataTest()
        {
            var key = Hash.Generate();
            var data = Hash.Generate().Value.ToArray();
            await _worldStateManager.SetDataAsync(key, data);

            var getData = await _worldStateManager.GetDataAsync(key);
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        public async Task AccountDataProviderTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var address = Hash.Generate();

            await _worldStateManager.OfChain(chain.Id);
            
            var accountDataProvider = _worldStateManager.GetAccountDataProvider(address);
            
            Assert.True(accountDataProvider.Context.Address == address);
            Assert.True(accountDataProvider.Context.ChainId == chain.Id);
            
            var dataProvider = accountDataProvider.GetDataProvider();
            var data = Hash.Generate().Value.ToArray();
            var key = new Hash("testkey".CalculateHash());
            await dataProvider.SetAsync(key, data);
            var getData = await dataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getData));
        }
    }
}