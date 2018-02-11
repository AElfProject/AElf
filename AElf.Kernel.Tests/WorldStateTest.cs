using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class WorldStateTest
    {
        /// <summary>
        /// Use GetAccountDataProviderByAccount method to get an already set account data provider.
        /// </summary>
        [Fact]
        public void GetAccountDataProviderTest()
        {
            var worldState = new WorldState();
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var account = new Account(address);
            var accountDataProvider = new AccountDataProvider(account, worldState);

            var hashOriginAccountDataProvider = new Hash<IAccount>(accountDataProvider.CalculateHash());
            var getAccountDataProvider = worldState.GetAccountDataProviderByAccount(account);
            var hashGetAccountDataProvider = new Hash<IAccountDataProvider>(getAccountDataProvider.CalculateHash());
            
            Assert.True(hashGetAccountDataProvider.Equals(hashOriginAccountDataProvider));
        }
    }
}