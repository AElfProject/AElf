using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using AElf.Kernel.Types.Merkle;
using Akka.IO;
using Google.Protobuf;
using Moq;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Miner
{
    [UseAutofacTestFramework]
    public class BlockGeneration
    {
        private readonly IChainService _chainService;
        private readonly IStateDictator _stateDictator;

        public BlockGeneration(IChainService chainService, IStateDictator stateDictator)
        {
            _chainService = chainService;
            _stateDictator = stateDictator;
        }


        public async Task SetWorldState()
        {
            var address = Hash.Generate();
            var accountDataProvider = _stateDictator.GetAccountDataProvider(address);
            var dataProvider = accountDataProvider.GetDataProvider();
            var data1 = Hash.Generate().Value.ToArray();
            var key = new Hash("testkey".CalculateHash());
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
        }
        
    }
}