using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.SmartContract;
using Xunit.Frameworks.Autofac;

namespace AElf.Miner.Tests
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
            throw new NotImplementedException();
        }
        

//        public Mock<IChainManager> GetChainManager(Hash lastBlockHash)
//        {
//            var mock = new Mock<IChainManager>();
//            mock.Setup(c => c.GetChainLastBlockHash(It.IsAny<Hash>())).Returns(Task.FromResult(lastBlockHash));
//            return mock;
//        }

        
    }
}