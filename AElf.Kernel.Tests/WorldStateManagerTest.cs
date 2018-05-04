using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;
using Castle.Components.DictionaryAdapter;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class WorldStateManagerTest
    {
        private readonly IWorldStateManager _worldStateManager;

        private readonly Hash _genesisBlockHash = Hash.Generate();
        
        public WorldStateManagerTest(IWorldStateStore worldStateStore, IPointerStore pointerStore, IAccountContextService accountContextService, IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateManager = new WorldStateManager(worldStateStore, 
                accountContextService, pointerStore, changesStore, dataStore);
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

            await _worldStateManager.SetWorldStateToCurrentState(chain.Id, _genesisBlockHash);

            var accountDataProvider = _worldStateManager.GetAccountDataProvider(chain.Id, address);
            
            Assert.True(accountDataProvider.Context.Address == address);
            Assert.True(accountDataProvider.Context.ChainId == chain.Id);
            
            var dataProvider = accountDataProvider.GetDataProvider();
            var data = Hash.Generate().Value.ToArray();
            var key = new Hash("testkey".CalculateHash());
            await dataProvider.SetAsync(key, data);
            var getData = await dataProvider.GetAsync(key);
            
            Assert.True(data.SequenceEqual(getData));
        }

        [Fact]
        //TODO: Not finished.
        public async Task RollbackChangesTest()
        {
            var chain = new Chain(Hash.Generate());

            await _worldStateManager.SetWorldStateToCurrentState(chain.Id, _genesisBlockHash);

            var result = await FabricateAndExecuteATx(chain);
            var rollbackStart = result.Item1;
            var rollbackCount = result.Item2;

            await _worldStateManager.RollbackSeveralChanges(rollbackStart, rollbackCount);

            var changedPaths = await _worldStateManager.GetPathsAsync();
        }


        private async Task<Tuple<long, int>> FabricateAndExecuteATx(Chain chain)
        {
            long start = 0;
            var count = 0;
            
            var address = Hash.Generate();
            var accountDataProvider = _worldStateManager.GetAccountDataProvider(chain.Id, address);
            var dataProvider = accountDataProvider.GetDataProvider();

            List<byte[]> values = new EditableList<byte[]>();
            for (var i = 0; i < 5; i++)
            {
                values.Add(Hash.Generate().Value.ToArray());
            }

            foreach (var value in values)
            {
                var key = Hash.Generate();
                start = count == 0 ? await dataProvider.SetAsync(key, value) : start;
                count++;
            }
            
            return Tuple.Create(start, count);
        }
    }
}