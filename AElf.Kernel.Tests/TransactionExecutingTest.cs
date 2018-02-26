using AElf.Kernel.Extensions;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionExecutingTest
    {
        
        private readonly WorldState _worldState = new WorldState();

        private IChain CreateChain(ITransaction transactionInGenesisBlock)
        {
            var genesisBlock = new GenesisBlock {Transaction = transactionInGenesisBlock};
            var chain = new Chain(_worldState, genesisBlock);

            return chain;
        }

        private ITransaction CreateTransactionInGenesisBlock(int category, string name, out byte[] data)
        {
            var transactionInGenesisBlock = new Transaction {Params = new object[3]};
            transactionInGenesisBlock.Params[0] = category;
            transactionInGenesisBlock.Params[1] = name;
            
            // load assembly
            var path = @"/../../../Assembly/SimpleClass.dll";
            data = System.IO.File.ReadAllBytes(System.IO.Directory.GetCurrentDirectory() + path);
            transactionInGenesisBlock.Params[2] = data;

            return transactionInGenesisBlock;
        }
        
        
        /// <summary>
        /// test case for creating account
        /// </summary>
        [Fact]
        public void CreateAccountTest()
        {
            /*
             * 1. deploy accountZero
             * 2. create new account with contract in accountZero
             */

            const string name = "SmartContractInitialization";
            const int category = 0;

            var transactionInGenesisBlock = CreateTransactionInGenesisBlock(category, name, out var data);
            // create chain 
            var chain = (Chain)CreateChain(transactionInGenesisBlock);
            // accountZero deployment
            Assert.True(chain.Initialize());
            
            var accountManager = chain.AccountManager;
            var accountZero = chain.AccountZero;
            
            // create a caller account
            var callerAccount = accountManager.CreateAccount();
            // create the new account
            var newAccount = accountManager.CreateAccount(callerAccount, name).Result;

            var accountDataProvider = _worldState.GetAccountDataProviderByAccount(newAccount);
            Assert.NotNull(accountDataProvider);
            
            const string smartContractMapKey = "SmartContractMap";
            var smartContractRegistration =
                (SmartContractRegistration) accountDataProvider.GetDataProvider()
                    .GetDataProvider(smartContractMapKey)
                    .GetAsync(new Hash<SmartContractRegistration>(
                        accountZero.CalculateHashWith(name)))
                    .Result;
            
            Assert.Equal(smartContractRegistration.Category, category);
            Assert.Equal(smartContractRegistration.Name, name);
            Assert.True(smartContractRegistration.Bytes == data);

        }
        
    }
}