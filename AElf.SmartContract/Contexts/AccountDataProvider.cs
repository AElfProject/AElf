using System;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IStateDictator _stateDictator;
        private readonly DataPath _dataPath;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountAddress, IStateDictator stateDictator)
        {
            _stateDictator = stateDictator;

            _dataPath = new DataPath()
                .SetChainId(stateDictator.ChainId)
                .SetRoundNumber(stateDictator.CurrentRoundNumber)
                .SetBlockProducerAddress(stateDictator.BlockProducerAccountAddress)
                .SetAccountAddress(accountAddress);
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(_dataPath, _stateDictator);
        }
    }
}
