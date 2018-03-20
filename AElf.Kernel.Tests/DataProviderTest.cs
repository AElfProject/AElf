using AElf.Kernel.Extensions;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class DataProviderTest
    {
        /// <summary>
        /// Use an account data provider to get corresponding account’s address.
        /// </summary>
        [Fact]
        public void GetAccountAddressTest()
        {
            //Initialization.
            var worldState = new WorldState();
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var account = new Account(address);
            var accountDataProvider = new AccountDataProvider(account, worldState);

            var getAddress = accountDataProvider.GetAccountAddress();
            
            //See if the addresses set to the account and get from account data provider are equal.
            Assert.True(address.Equals(getAddress));
        }

        /// <summary>
        /// Get data provider from account data provider and data provider.
        /// </summary>
        [Fact]
        public void GetDataProviderTest()
        {
            //Initialization.
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var accountDataProvider = CreateAccountDataProvider(address);

            var dataProviderFromAccountDataProvider = accountDataProvider.GetDataProvider();
            
            //See if the GetDataProvider method return a DataProvider instance.
            Assert.True(dataProviderFromAccountDataProvider.GetType() == typeof(DataProvider));

            var dataPrivderFromDataProvider = dataProviderFromAccountDataProvider.GetDataProvider("test");
            
            //See if the GetDataProvider method return a DataProvider instance.
            Assert.True(dataPrivderFromDataProvider.GetType() == typeof(DataProvider));
        }

        /// <summary>
        /// Use SetDataProvider to set a data provider, then get this data provider.
        /// </summary>
        [Fact]
        public void GetSetDataProvider()
        {
            //Initializaton.
            var worldState = new WorldState();
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var account = new Account(address);
            var accountDataProvider = new AccountDataProvider(account, worldState);
            var dataprovider = accountDataProvider.GetDataProvider();

            const string dataProviderName = "test";
            
            dataprovider.SetDataProvider(dataProviderName);

            var hashNewDataProvider = new Hash<IDataProvider>(new DataProvider(worldState, address).CalculateHash());
            var hashGetDataProvider = new Hash<IDataProvider>(dataprovider.GetDataProvider(dataProviderName).CalculateHash());
            
            //See if we can get the same data provider which set before by comparing their hashes.
            Assert.True(hashGetDataProvider.Equals(hashNewDataProvider));
        }

        /// <summary>
        /// Use SetAsync method to set a serialized data and check the change of merkle tree.
        /// </summary>
        [Fact]
        public void StoreDataToWorldStateTest()
        {
            //Initialization.
            var worldState = new WorldState();
            var address = new Hash<IAccount>("aelf".CalculateHash());
            var account = new Account(address);
            var accountDataProvider = new AccountDataProvider(account, worldState);
            var dataprovider = accountDataProvider.GetDataProvider();
            
            //Add a data provider to world state merkle tree.
            worldState.AddAccountDataProvider(accountDataProvider);
            
            //Merkle tree root hash before set:
            var merkleHashBefore = worldState.GetWorldStateMerkleTreeRootAsync();

            //Set a data.
            var hashKey = new Hash<string>(address.CalculateHashWith("AnySerializedData"));
            ITransaction obj = new Transaction()
            {
                From = new Account(address),
            };
            /*dataprovider.SetAsync(hashKey, obj);

            //Merkle tree root hash after set:
            var merkleHashAfter = worldState.GetWorldStateMerkleTreeRootAsync();
            
            var getData = dataprovider.GetAsync(hashKey).Result;
            
            //See if get the same data which set before.
            Assert.True(getData == obj);
            //See if the merkle tree root has changed after set a new data.
            Assert.True(!merkleHashAfter.Equals(merkleHashBefore));*/
        }

        #region Some useful methods.

        /// <summary>
        /// Use an address to create an account data provider.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private AccountDataProvider CreateAccountDataProvider(Hash<IAccount> address)
        {
            var worldState = new WorldState();
            var account = new Account(address);
            return new AccountDataProvider(account, worldState);
        }

        #endregion

    }
}