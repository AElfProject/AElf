using System.Linq;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.TxMemPool;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateDictatorTest
    {
        private readonly IStateDictator _stateDictator;
        private readonly ILogger _logger;
        private readonly BlockTest _blockTest;

        public WorldStateDictatorTest(IDataStore dataStore, BlockTest blockTest, ILogger logger)
        {
            _stateDictator = new StateDictator(dataStore, _logger)
            {
                BlockProducerAccountAddress = Hash.Generate()
            };
            _blockTest = blockTest;
            _logger = logger;
        }

        [Fact]
        public async Task DataTest()
        {
            var key = Hash.Generate();
            var data = Hash.Generate().Value.ToArray();
            await _stateDictator.SetDataAsync(key, data);

            var getData = await _stateDictator.GetDataAsync(key);
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        public async Task AccountDataProviderTest()
        {
            var chain = await _blockTest.CreateChain();
            
            var address = Hash.Generate();

            _stateDictator.SetChainId(chain.Id);
            
            var accountDataProvider = await _stateDictator.GetAccountDataProvider(address);
            
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