using System.Collections.Generic;
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
    public class DataProviderTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChainStore _chainStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public DataProviderTest(IWorldStateStore worldStateStore, IChainStore chainStore, IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _chainStore = chainStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        [Fact]
        public async Task SetTest()
        {
            const int count = 5;
            var setList = CreateSet(count).ToList();
            var keys = GenerateKeys(setList).ToList();

            var genesisBlockHash = Hash.Generate();
            var chain = new Chain(Hash.Generate(), genesisBlockHash);
            var chainManager = new ChainManager(_chainStore, _dataStore);
            await chainManager.AddChainAsync(chain.Id, genesisBlockHash);

            var address = Hash.Generate();
            var worldStateManager = await new WorldStateManager(_worldStateStore, _changesStore, _dataStore).OfChain(chain.Id);
            await worldStateManager.SetWorldStateAsync(genesisBlockHash);
            var accountDataProvider = worldStateManager.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();

            for (var i = 0; i < count; i++)
            {
                await dataProvider.SetAsync(keys[i], setList[i]);
            }

            for (var i = 0; i < count; i++)
            {
                var getData = await dataProvider.GetAsync(keys[i]);
                Assert.True(getData.SequenceEqual(setList[i]));
            }

            for (var i = 0; i < count - 1; i++)
            {
                var getData = await dataProvider.GetAsync(keys[i]);
                Assert.False(getData.SequenceEqual(setList[i + 1]));
            }
        }
        
        private IEnumerable<byte[]> CreateSet(int count)
        {
            var list = new List<byte[]>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(Hash.Generate().Value.ToByteArray());
            }

            return list;
        }

        private IEnumerable<Hash> GenerateKeys(IEnumerable<byte[]> set)
        {
           return set.Select(data => new Hash(data.CalculateHash())).ToList();
        }
    }
}