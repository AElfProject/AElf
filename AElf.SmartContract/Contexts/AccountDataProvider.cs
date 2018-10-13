using System;
using AElf.Kernel;
using AElf.Common;
using AElf.Kernel.Storages;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IStateDictator _stateDictator;
        private readonly IStateStore _stateStore;
        private readonly DataPath _dataPath;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Address accountAddress, IStateDictator stateDictator, IStateStore stateStore)
        {
            _stateDictator = stateDictator;
            _stateStore = stateStore;

            _dataPath = new DataPath()
                .SetChainId(stateDictator.ChainId)
                .SetBlockHeight(stateDictator.BlockHeight)
                .SetBlockProducerAddress(stateDictator.BlockProducerAccountAddress)
                .SetAccountAddress(accountAddress);
        }

        public IDataProvider GetDataProvider()
        {
            //Console.WriteLine($"ChainId: {_dataPath.ChainId.ToHex()}");
            //Console.WriteLine($"Block Height: {_dataPath.BlockHeight}");
            //Console.WriteLine($"BP Address: {_dataPath.BlockProducerAddress.ToHex()}");
            //Console.WriteLine($"Contract Address: {_dataPath.ContractAddress.ToHex()}");
            var dp = NewDataProvider.GetRootDataProvider(_stateDictator.ChainId, _dataPath.ContractAddress);
            dp.StateStore = _stateStore;
            return dp;
//            return new DataProvider(_dataPath, _stateDictator);
        }
    }
}
