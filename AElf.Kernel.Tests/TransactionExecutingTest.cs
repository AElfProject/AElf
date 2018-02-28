using System;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionExecutingTest
    {
        private readonly WorldState _worldState = new WorldState();
        private const string SmartContractMapKey = "SmartContractMap";
        private IChain CreateChain(ITransaction transactionInGenesisBlock, 
            out AccountZero accountZero, out AccountManager accountManager)
        {
            
            var smartContractZero = new SmartContractZero();
            accountZero = new AccountZero(smartContractZero);
            accountManager = new AccountManager(_worldState, accountZero);
            var genesisBlock = new GenesisBlock {Transaction = transactionInGenesisBlock};
            var chain = new Chain(genesisBlock, accountManager);

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


        private IAccount CreateAccount(string str)
        {
            var callerAccount = new Account(new Hash<IAccount>(this.CalculateHashWith(str)));
            return callerAccount;
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
            var chain = (Chain)CreateChain(transactionInGenesisBlock, out var accountZero, out var accountManager);
            // accountZero deployment
            Assert.True(chain.Initialize());
            
            // create a caller account, normal account
            var callerAccount = CreateAccount("account1");
            
            // create the new account
            var newAccount = CreateAccount("account2");
            
            // add an account data provider to world state
            _worldState.AddAccountDataProvider(new AccountDataProvider(callerAccount, _worldState));
            _worldState.AddAccountDataProvider(new AccountDataProvider(newAccount, _worldState));
            
            // initialize account 
            
            var smartContractManager = new SmartContractManager(accountManager);
            var accountDataProvider = _worldState.GetAccountDataProviderByAccount(newAccount);
            Assert.NotNull(accountDataProvider);

            
            // deployment smartcontract in accountZero to new account
            var smartContract = smartContractManager.GetAsync(newAccount).Result;
            smartContract.InitializeAsync(accountDataProvider);
            var param = new object[] {name};
            smartContract.InvokeAsync(callerAccount, "CreateAccount", param);
            
            // get the smartcontract in new account to verify correctness
            var smartContractRegistration =
                (SmartContractRegistration) accountDataProvider.GetDataProvider()
                    .GetDataProvider(SmartContractMapKey)
                    .GetAsync(new Hash<SmartContractRegistration>(
                        newAccount.GetAddress().CalculateHashWith(name)))
                    .Result;
            
            // type
            Assert.Equal(smartContractRegistration.Category, category);
            // contract name
            Assert.Equal(smartContractRegistration.Name, name);
            // contract data
            Assert.True(smartContractRegistration.Bytes == data);

        }



        [Fact]
        public void TransferTest()
        {
            
        }


        [Fact]
        public void DeployNewmrtContract()
        {
            
        }
        
    }
}