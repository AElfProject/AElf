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
            Console.WriteLine($"ChainId: {_dataPath.ChainId.ToHex()}");
            Console.WriteLine($"RoundNumber: {_dataPath.RoundNumber}");
            Console.WriteLine($"BP: {_dataPath.BlockProducerAddress.ToHex()}");
            Console.WriteLine($"ContractAddress: {_dataPath.ContractAddress.ToHex()}");
            return new DataProvider(_dataPath, _stateDictator);
        }
    }
}
