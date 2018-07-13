using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Kernel.TxMemPool;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class DataProviderTest
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;
        private readonly ILogger _logger;
        private readonly ITxPoolService _txPoolService;
        private readonly IBlockHeaderStore _blockHeaderStore;
        private readonly IBlockBodyStore _blockBodyStore;
        private readonly ITransactionStore _transactionStore;

        public DataProviderTest(IWorldStateStore worldStateStore,
            IChangesStore changesStore, IDataStore dataStore,
            BlockTest blockTest, ILogger logger,
            ITxPoolService txPoolService, IBlockHeaderStore blockHeaderStore, IBlockBodyStore blockBodyStore,
            ITransactionStore transactionStore)
        {
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
            _blockTest = blockTest;
            _logger = logger;
            _txPoolService = txPoolService;
            _blockHeaderStore = blockHeaderStore;
            _blockBodyStore = blockBodyStore;
            _transactionStore = transactionStore;
        }

        [Fact]
        public async Task SetTest()
        {
            const int count = 5;
            var setList = CreateSet(count).ToList();
            var keys = GenerateKeys(setList).ToList();

            var chain = await _blockTest.CreateChain();

            var address = Hash.Generate();

            var worldStateDictator = new WorldStateDictator(_worldStateStore, _changesStore, _dataStore,
                _blockHeaderStore, _blockBodyStore, _transactionStore, _logger).SetChainId(chain.Id);
            worldStateDictator.BlockProducerAccountAddress = Hash.Generate();

            await worldStateDictator.SetWorldStateAsync(chain.GenesisBlockHash);
            
            var accountDataProvider = await worldStateDictator.GetAccountDataProvider(address);
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
                list.Add(Hash.Generate().GetHashBytes());
            }

            return list;
        }

        private IEnumerable<Hash> GenerateKeys(IEnumerable<byte[]> set)
        {
           return set.Select(data => new Hash(data.CalculateHash())).ToList();
        }
    }
}