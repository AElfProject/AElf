using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateManagerTest
    {
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly ILogger _logger;
        private readonly Hash _accountAddress;
        
        private readonly BlockTest _blockTest;

        public WorldStateManagerTest(IWorldStateStore worldStateStore, IChangesStore changesStore, 
            IDataStore dataStore, BlockTest blockTest, ILogger logger, Hash accountAddress)
        {
            _worldStateDictator = new WorldStateDictator(worldStateStore, changesStore, dataStore, _logger, _accountAddress);
            _blockTest = blockTest;
            _logger = logger;
            _accountAddress = accountAddress;
        }

        [Fact]
        public async Task DataTest()
        {
            var key = Hash.Generate();
            var data = Hash.Generate().Value.ToArray();
            await _worldStateDictator.SetDataAsync(key, data);

            var getData = await _worldStateDictator.GetDataAsync(key);
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        public async Task AccountDataProviderTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var address = Hash.Generate();

            _worldStateDictator.SetChainId(chain.Id);
            
            var accountDataProvider = await _worldStateDictator.GetAccountDataProvider(address);
            
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