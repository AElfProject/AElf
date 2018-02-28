using System;
using System.Reflection;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class DeployAccountZeroTest
    {
        
        private readonly WorldState _worldState = new WorldState();
        private const string SmartContractMapKey = "SmartContractMap";
        private IChain CreateChain(ITransaction transactionInGenesisBlock, out AccountZero accountZero)
        {
            
            var smartContractZero = new SmartContractZero();
            accountZero = new AccountZero(smartContractZero);
            var accountManager = new AccountManager(_worldState, accountZero);
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
        
        [Fact]
        public void DeployAccountZero()
        {
            const string name = "SmartContractInitialization";
            const int category = 0;

            var transactionInGenesisBlock = CreateTransactionInGenesisBlock(category, name, out var data);
            // create chain 
            var chain = (Chain)CreateChain(transactionInGenesisBlock, out var accountZero);
            
            
            // accountZero deployment
            Assert.True(chain.Initialize());
            // deployment only once
            Assert.False(chain.Initialize());
            // only genesis block in the chain
            Assert.Equal(chain.CurrentBlockHeight, 1);
            
            var accountZeroDataProvider = _worldState.GetAccountDataProviderByAccount(accountZero);
            Assert.NotNull(accountZeroDataProvider);

            var scrHash = new Hash<SmartContractRegistration>(Hash<IAccount>.Zero.CalculateHashWith(name));
            var smartContractRegistration =
                (SmartContractRegistration) accountZeroDataProvider.GetDataProvider()
                    .GetDataProvider(SmartContractMapKey)
                    .GetAsync(scrHash)
                    .Result;
            
            Assert.Equal(smartContractRegistration.Category, category);
            Assert.Equal(smartContractRegistration.Name, name);
            Assert.True(smartContractRegistration.Bytes == data);
            
        }
    }
}