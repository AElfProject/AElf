using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using NLog;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class DataProviderTest
    {
        private readonly IDataStore _dataStore;
        private readonly BlockTest _blockTest;
        private readonly ILogger _logger;

        public DataProviderTest(IDataStore dataStore, BlockTest blockTest, ILogger logger)
        {
            _dataStore = dataStore;
            _blockTest = blockTest;
            _logger = logger;
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
           return set.Select(Hash.FromBytes).ToList();
        }
    }
}