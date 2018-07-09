using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Node;
using AElf.Kernel.Services;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly Hash _blockProducerAccountAddress;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash chainId, Hash accountAddress, 
            IWorldStateDictator worldStateDictator, Hash blockProducerAccountAddress)
        {
            _worldStateDictator = worldStateDictator;
            _blockProducerAccountAddress = blockProducerAccountAddress;

            //Just use its structure to store info.
            Context = new AccountDataContext
            {
                Address = accountAddress,
                ChainId = chainId
            };

        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateDictator, _blockProducerAccountAddress);
        }
    }
}
