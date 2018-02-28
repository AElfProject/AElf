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
            worldState.AddAccountDataProvider(accountDataProvider);

            var hashOriginAccountDataProvider = new Hash<IAccount>(accountDataProvider.CalculateHash());
            var getAccountDataProvider = worldState.GetAccountDataProviderByAccount(account);
            var hashGetAccountDataProvider = new Hash<IAccountDataProvider>(getAccountDataProvider.CalculateHash());
            
            //See if we can get the same data provider which we set before.
            Assert.True(hashGetAccountDataProvider.Equals(hashOriginAccountDataProvider));
        }

        /// <summary>
        /// Add some data providers to world state and compare the merkle tree root hash every time.
        /// </summary>
        [Fact]
        public void WorldStateMerkleTreeRootTest()
        {
            var worldState = new WorldState();
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var account = new Account(address);
            var accountDataProvider = new AccountDataProvider(account, worldState);
            var dataProvider = accountDataProvider.GetDataProvider();
            
            //Add a data provider to world state merkle tree.
            worldState.AddAccountDataProvider(accountDataProvider);

            var merkleTreeRootHashBefore = worldState.GetWorldStateMerkleTreeRootAsync().Result;

            var newDataProvider = new DataProvider(worldState, account.GetAddress());
            dataProvider.SetDataProvider("SubDataProviderForTest", newDataProvider);

            var merkleTreeRootHashAfter = worldState.GetWorldStateMerkleTreeRootAsync().Result;
            
            //See if the merkle tree root hash changed after set a new data provider.
            Assert.True(!merkleTreeRootHashAfter.Equals(merkleTreeRootHashBefore));

            newDataProvider.SetDataProvider("SubSubDataProviderForTest",
                new DataProvider(worldState, account.GetAddress()));
            
            //See if the merkle tree root hash changed after set a new data provider again.
            Assert.True(!merkleTreeRootHashAfter.Equals(merkleTreeRootHashBefore));
        }
    }
}