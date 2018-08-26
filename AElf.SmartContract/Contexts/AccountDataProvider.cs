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
                .SetBlockHeight(stateDictator.BlockHeight)
                .SetBlockProducerAddress(stateDictator.BlockProducerAccountAddress)
                .SetAccountAddress(accountAddress);
        }

        public IDataProvider GetDataProvider()
        {
            Console.WriteLine($"ChainId: {_dataPath.ChainId.ToHex()}");
            Console.WriteLine($"Block Height: {_dataPath.BlockHeight}");
            Console.WriteLine($"BP Address: {_dataPath.BlockProducerAddress.ToHex()}");
            Console.WriteLine($"Contract Address: {_dataPath.ContractAddress.ToHex()}");
            return new DataProvider(_dataPath, _stateDictator);
        }
    }
}
