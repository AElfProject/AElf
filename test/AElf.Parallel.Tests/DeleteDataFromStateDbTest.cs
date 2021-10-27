using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Parallel.Tests
{
    //TODO: rewrite this class, too many repeated codes
    public sealed class DeleteDataFromStateDbTest : AElfIntegratedTest<ParallelTestWithoutFeeAElfModule>
    {
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBlockAttachService _blockAttachService;
        private readonly IAccountService _accountService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IStateStore<VersionedState> _versionedStates;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly INonparallelContractCodeProvider _nonparallelContractCodeProvider;
        private readonly ITransactionGrouper _transactionGrouper;
        private readonly ParallelTestHelper _parallelTestHelper;

        public DeleteDataFromStateDbTest()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _accountService = GetRequiredService<IAccountService>();
            _versionedStates = GetRequiredService<IStateStore<VersionedState>>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _nonparallelContractCodeProvider = GetRequiredService<INonparallelContractCodeProvider>();
            _transactionGrouper = GetRequiredService<ITransactionGrouper>();
            _parallelTestHelper = GetRequiredService<ParallelTestHelper>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task Set_Value()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValue), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValue), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_Inline()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInline), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInline), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPrePlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPrePlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPostPlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPostPlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_Inline_And_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPrePlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPrePlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_Inline_And_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPostPlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPostPlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_With_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            //NonParallel System Transaction
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 4,
                StringValue = "4"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 4,
                StringValue = "4"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_Parallel_With_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var transactions = new List<Transaction>();

            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };
            
            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), increaseValueInput);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), otherKeyInput);
            transactions.Add(transaction);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 4,
                StringValue = "4"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var otherMessageValue = new MessageValue
            {
                Int64Value = 4,
                StringValue = "4"
            };
            value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
            CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(6);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Set_Value_In_Blocks()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            const string otherKey = "OtherKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, otherKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var increaseValueInput = new IncreaseValueInput
            {
                Key = key,
                Memo = Guid.NewGuid().ToString()
            };
            
            var otherKeyInput = new IncreaseValueInput
            {
                Key = otherKey,
                Memo = Guid.NewGuid().ToString()
            };

            MessageValue messageValue;
            MessageValue otherMessageValue;

            //First block
            {
                var systemTransactions = new List<Transaction>();
                var transactions = new List<Transaction>();

                //NonParallel System Transaction
                {
                    var systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), otherKeyInput);
                    systemTransactions.Add(systemTransaction);
                }
                
                //Parallel System Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), otherKeyInput);
                    systemTransactions.Add(systemTransaction);
                }
                
                var groupedSystemTransactions = await _transactionGrouper.GroupAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, systemTransactions);
                groupedSystemTransactions.Parallelizables.Count.ShouldBe(2);
                groupedSystemTransactions.Parallelizables[0].Count.ShouldBe(2);
                groupedSystemTransactions.Parallelizables[1].Count.ShouldBe(1);
                groupedSystemTransactions.NonParallelizables.Count.ShouldBe(2);
                
                //NonParallel Normal Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    transactions.Add(transaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    transactions.Add(transaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), otherKeyInput);
                    transactions.Add(transaction);
                }

                //Parallel Normal Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    transactions.Add(transaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    transactions.Add(transaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), otherKeyInput);
                    transactions.Add(transaction);
                }
                
                var groupedNormalTransactions = await _transactionGrouper.GroupAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, transactions);
                groupedNormalTransactions.Parallelizables.Count.ShouldBe(2);
                groupedNormalTransactions.Parallelizables[0].Count.ShouldBe(2);
                groupedNormalTransactions.Parallelizables[1].Count.ShouldBe(1);
                groupedNormalTransactions.NonParallelizables.Count.ShouldBe(3);
                
                var allTransactions = systemTransactions.Concat(transactions).ToList();
                var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
                    allTransactions);
                block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions, transactions,
                    CancellationToken.None)).Block;
                await _blockchainService.AddTransactionsAsync(allTransactions);
                await _blockchainService.AddBlockAsync(block);
                await _blockAttachService.AttachBlockAsync(block);

                var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
                transactionResults.ShouldAllBe(t => t.Status == TransactionResultStatus.Mined);

                var nonparallelContractCode = await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                    new ChainContext
                    {
                        BlockHash = block.GetHash(),
                        BlockHeight = block.Height
                    }, ParallelTestHelper.BasicFunctionWithParallelContractAddress);
                nonparallelContractCode.ShouldBeNull();
                    
                messageValue = new MessageValue
                {
                    Int64Value = 28,
                    StringValue = "28"
                };
                value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
                CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);

                otherMessageValue = new MessageValue
                {
                    Int64Value = 16,
                    StringValue = "16"
                };
                value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
                CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);

                var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
                blockStateSet.Changes.Count.ShouldBe(6);
                blockStateSet.Deletes.Count.ShouldBe(0);
            }

            {
                chain = await _blockchainService.GetChainAsync();
                var systemTransactions = new List<Transaction>();
                var transactions = new List<Transaction>();

                //NonParallel System Transaction
                {
                    var systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), otherKeyInput);
                    systemTransactions.Add(systemTransaction);
                }
                

                //Parallel System Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(systemTransaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    systemTransaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), otherKeyInput);
                    systemTransactions.Add(systemTransaction);
                }
                
                var groupedSystemTransactions = await _transactionGrouper.GroupAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, systemTransactions);
                groupedSystemTransactions.Parallelizables.Count.ShouldBe(2);
                groupedSystemTransactions.Parallelizables[0].Count.ShouldBe(2);
                groupedSystemTransactions.Parallelizables[1].Count.ShouldBe(1);
                groupedSystemTransactions.NonParallelizables.Count.ShouldBe(2);

                //NonParallel Normal Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    transactions.Add(transaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    transactions.Add(transaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), otherKeyInput);
                    transactions.Add(transaction);
                }
                
                //Parallel Normal Transaction
                {
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    transactions.Add(transaction);

                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    transactions.Add(transaction);

                    otherKeyInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), otherKeyInput);
                    transactions.Add(transaction);
                }
                
                var groupedNormalTransactions = await _transactionGrouper.GroupAsync(new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, transactions);
                groupedNormalTransactions.Parallelizables.Count.ShouldBe(2);
                groupedNormalTransactions.Parallelizables[0].Count.ShouldBe(2);
                groupedNormalTransactions.Parallelizables[1].Count.ShouldBe(1);
                groupedNormalTransactions.NonParallelizables.Count.ShouldBe(3);
                
                var allTransactions = systemTransactions.Concat(transactions).ToList();
                var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
                    allTransactions);
                block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions, transactions,
                    CancellationToken.None)).Block;
                await _blockchainService.AddTransactionsAsync(allTransactions);
                await _blockchainService.AddBlockAsync(block);
                await _blockAttachService.AttachBlockAsync(block);

                var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
                transactionResults.ShouldAllBe(t => t.Status == TransactionResultStatus.Mined);

                var nonparallelContractCode = await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                    new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                    ParallelTestHelper.BasicFunctionWithParallelContractAddress);
                nonparallelContractCode.ShouldBeNull();

                messageValue = new MessageValue
                {
                    Int64Value = 56,
                    StringValue = "56"
                };
                value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
                CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);

                otherMessageValue = new MessageValue
                {
                    Int64Value = 32,
                    StringValue = "32"
                };
                value = await GetValueAsync(accountAddress, otherKey, block.GetHash(), block.Height);
                CheckValue(value, otherMessageValue.StringValue, otherMessageValue.Int64Value, otherMessageValue);

                var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
                blockStateSet.Changes.Count.ShouldBe(6);
                blockStateSet.Deletes.Count.ShouldBe(0);
            }


            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value,
                messageValue.StringValue, messageValue);
            await CheckValueInVersionStateAsync(otherKey, otherMessageValue.Int64Value,
                otherMessageValue.StringValue, otherMessageValue);
        }
        
        [Fact]
        public async Task Remove_Not_Exist_Key()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValue), new RemoveValueInput
                {
                    Key = key
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(3);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Remove_After_Set_Key()
        {
            const string key = "TestKey";
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveAfterSetValue), new RemoveAfterSetValueInput
                {
                    Key = key,
                    Int64Value = 10,
                    StringValue = "test",
                    MessageValue = new MessageValue
                    {
                        Int64Value = 20,
                        StringValue = "MessageTest",
                    }
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(3);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Set_After_Remove_Key()
        {
            const string key = "TestKey";
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var setValueInput = new SetValueInput
            {
                Key = key,
                Int64Value = 10,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    Int64Value = 20,
                    StringValue = "MessageTest",
                }
            };
            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.SetValue), setValueInput);
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, setValueInput.StringValue, setValueInput.Int64Value,
                setValueInput.MessageValue);
            
            var setAfterRemoveValueInput = new SetAfterRemoveValueInput
            {
                Key = key,
                Int64Value = 20,
                StringValue = "test2",
                MessageValue = new MessageValue
                {
                    Int64Value = 10,
                    StringValue = "MessageTest2",
                }
            };
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.SetAfterRemoveValue), setAfterRemoveValueInput);
            transactions = new List<Transaction> {transaction};
            block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, setAfterRemoveValueInput.StringValue,
                setAfterRemoveValueInput.Int64Value, setAfterRemoveValueInput.MessageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key,
                setAfterRemoveValueInput.Int64Value, setAfterRemoveValueInput.StringValue,
                setAfterRemoveValueInput.MessageValue);
        }

        [Fact]
        public async Task Complex_ChangeAndDelete_Key()
        {
            const string key = "TestKey";
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            //ComplexChangeWithDeleteValue1
            var complexChangeInput = new SetValueInput
            {
                Key = key,
                Int64Value = 100,
                StringValue = "test1",
                MessageValue = new MessageValue
                {
                    Int64Value = 100,
                    StringValue = "test1"
                }
            };
            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.ComplexChangeWithDeleteValue1), complexChangeInput);
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, complexChangeInput.StringValue, complexChangeInput.Int64Value,
                null);
            
            //ComplexChangeWithDeleteValue2
            complexChangeInput = new SetValueInput
            {
                Key = key,
                Int64Value = 100,
                StringValue = "test2",
                MessageValue = new MessageValue
                {
                    Int64Value = 100,
                    StringValue = "test2"
                }
            };
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.ComplexChangeWithDeleteValue2), complexChangeInput);
            transactions = new List<Transaction> {transaction};
            block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, complexChangeInput.StringValue, 0,
                complexChangeInput.MessageValue);
            
            //ComplexChangeWithDeleteValue3
            complexChangeInput = new SetValueInput
            {
                Key = key,
                Int64Value = 100,
                StringValue = "test3",
                MessageValue = new MessageValue
                {
                    Int64Value = 100,
                    StringValue = "test3"
                }
            };
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.ComplexChangeWithDeleteValue3), complexChangeInput);
            transactions = new List<Transaction> {transaction};
            block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, "", complexChangeInput.Int64Value,
                null);
        }

        [Fact]
        public async Task Remove_Value_From_PrePlugin()
        {
            await Set_Value();
            
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();

            const string key = "TestKey";

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPrePlugin), new RemoveValueInput
                {
                    Key = key
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var messageValue = new MessageValue
            {
                Int64Value = 3,
                StringValue = "3"
            };
            
            var value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }

        [Fact]
        public async Task Remove_Value_With_Plugin()
        {
            await Set_Value();
            
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();

            const string key = "TestKey";
            
            var transactions = new List<Transaction>();
            
            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueWithPlugin), new RemoveValueInput
                {
                    Key = key
                });
            transactions.Add(transaction);
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            
            var value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Remove_Value_From_Inline_With_Plugin()
        {
            await Set_Value();
            
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();

            const string key = "TestKey";

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromInlineWithPlugin), new RemoveValueInput
                {
                    Key = key
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            
            var value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Remove_Value_From_PostPlugin()
        {
            await Set_Value();
            
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();

            const string key = "TestKey";

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), new RemoveValueInput
                {
                    Key = key
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(3);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Remove_Value_In_Blocks()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            var keys = new[] {"TestKeyOne", "TestKeyTwo", "TestKeyThree", "TestKeyFour", "TestKeyFive", "TestKeySix", "TestKeySeven"};
            
            var value = await GetValueAsync(accountAddress, keys[0], chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, keys[1], chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var tasks = keys.Select(key => GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                }));
            var transactions = await Task.WhenAll(tasks);
            
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions.ToList())).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
            transactionResults.ShouldAllBe(t=>t.Status == TransactionResultStatus.Mined);

            var messageValue = new MessageValue
            {
                Int64Value = 4,
                StringValue = "4"
            };
            foreach (var key in keys)
            {
                value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
                CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            }
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            //First Block
            {
                var systemTransactions = new List<Transaction>();
                var normalTransactions = new List<Transaction>();

                //System Transaction
                {
                    var increaseValueInput = new IncreaseValueInput
                    {
                        Key = keys[0],
                        Memo = Guid.NewGuid().ToString()
                    };

                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    var removeValueInput = new RemoveValueInput
                    {
                        Key = keys[0],
                        Memo = Guid.NewGuid().ToString()
                    };

                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[1];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[2];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[3];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[4];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(transaction);

                    removeValueInput.Key = keys[3];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueParallelFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);

                    removeValueInput.Key = keys[4];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueParallelFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);
                }

                //Normal Transaction
                {
                    var removeValueInput = new RemoveValueInput
                    {
                        Key = keys[2],
                        Memo = Guid.NewGuid().ToString()
                    };

                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    removeValueInput.Key = keys[5];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    removeValueInput.Key = keys[6];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    var increaseValueInput = new IncreaseValueInput
                    {
                        Key = keys[5],
                        Memo = Guid.NewGuid().ToString()
                    };
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), increaseValueInput);
                    normalTransactions.Add(transaction);

                    increaseValueInput.Key = keys[6];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), increaseValueInput);
                    normalTransactions.Add(transaction);
                }

                var allTransactions = systemTransactions.Concat(normalTransactions).ToList();
                block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, allTransactions);
                block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
                    normalTransactions,
                    CancellationToken.None)).Block;
                await _blockchainService.AddTransactionsAsync(systemTransactions);
                await _blockchainService.AddBlockAsync(block);
                await _blockAttachService.AttachBlockAsync(block);

                transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
                transactionResults.ShouldAllBe(t => t.Status == TransactionResultStatus.Mined);

                value = await GetValueAsync(accountAddress, keys[0], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                var newMessageValue = new MessageValue
                {
                    Int64Value = 8,
                    StringValue = "8"
                };
                value = await GetValueAsync(accountAddress, keys[1], block.GetHash(), block.Height);
                CheckValue(value, newMessageValue.StringValue, newMessageValue.Int64Value, newMessageValue);

                value = await GetValueAsync(accountAddress, keys[2], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[3], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[4], block.GetHash(), block.Height);
                CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);

                value = await GetValueAsync(accountAddress, keys[5], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[6], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
                blockStateSet.Changes.Count.ShouldBe(6);
                blockStateSet.Deletes.Count.ShouldBe(15);
            }
            
            //Second Block
            {
                var systemTransactions = new List<Transaction>();
                var normalTransactions = new List<Transaction>();

                //System Transaction
                {
                    var increaseValueInput = new IncreaseValueInput
                    {
                        Key = keys[0],
                        Memo = Guid.NewGuid().ToString()
                    };

                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    var removeValueInput = new RemoveValueInput
                    {
                        Key = keys[0],
                        Memo = Guid.NewGuid().ToString()
                    };

                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[1];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[2];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin), increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[3];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(transaction);

                    increaseValueInput.Key = keys[4];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPlugin),
                        increaseValueInput);
                    systemTransactions.Add(transaction);

                    removeValueInput.Key = keys[3];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueParallelFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);

                    removeValueInput.Key = keys[4];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueParallelFromPostPlugin), removeValueInput);
                    systemTransactions.Add(transaction);
                }

                //Normal Transaction
                {
                    var removeValueInput = new RemoveValueInput
                    {
                        Key = keys[2],
                        Memo = Guid.NewGuid().ToString()
                    };

                    var transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);
                    
                    removeValueInput = new RemoveValueInput
                    {
                        Key = keys[1],
                        Memo = Guid.NewGuid().ToString()
                    };

                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueParallelFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    removeValueInput.Key = keys[5];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    removeValueInput.Key = keys[6];
                    removeValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.RemoveValueFromPostPlugin), removeValueInput);
                    normalTransactions.Add(transaction);

                    var increaseValueInput = new IncreaseValueInput
                    {
                        Key = keys[5],
                        Memo = Guid.NewGuid().ToString()
                    };
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), increaseValueInput);
                    normalTransactions.Add(transaction);

                    increaseValueInput.Key = keys[6];
                    increaseValueInput.Memo = Guid.NewGuid().ToString();
                    transaction = await GenerateTransactionAsync(accountAddress,
                        ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                        nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithInlineAndPlugin), increaseValueInput);
                    normalTransactions.Add(transaction);
                }

                var allTransactions = systemTransactions.Concat(normalTransactions).ToList();
                block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, allTransactions);
                block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
                    normalTransactions,
                    CancellationToken.None)).Block;
                await _blockchainService.AddTransactionsAsync(systemTransactions);
                await _blockchainService.AddBlockAsync(block);
                await _blockAttachService.AttachBlockAsync(block);

                transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
                transactionResults.ShouldAllBe(t => t.Status == TransactionResultStatus.Mined);

                value = await GetValueAsync(accountAddress, keys[0], block.GetHash(), block.Height);
                CheckValueNotExisted(value);
                
                value = await GetValueAsync(accountAddress, keys[1], block.GetHash(), block.Height);
                CheckValueNotExisted(value);
                
                value = await GetValueAsync(accountAddress, keys[2], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[3], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[4], block.GetHash(), block.Height);
                CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);

                value = await GetValueAsync(accountAddress, keys[5], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                value = await GetValueAsync(accountAddress, keys[6], block.GetHash(), block.Height);
                CheckValueNotExisted(value);

                var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
                blockStateSet.Changes.Count.ShouldBe(3);
                blockStateSet.Deletes.Count.ShouldBe(18);
            }

            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(keys[0]);
            await CheckValueNotExistedInVersionStateAsync(keys[1]);
            await CheckValueNotExistedInVersionStateAsync(keys[2]);
            await CheckValueNotExistedInVersionStateAsync(keys[3]);
            await CheckValueNotExistedInVersionStateAsync(keys[5]);
            await CheckValueNotExistedInVersionStateAsync(keys[6]);
            await CheckValueInVersionStateAsync(keys[4], messageValue.Int64Value,
                messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Failed()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailed), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_Parallel_Failed()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelFailed), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Increase_Value_Failed_With_Inline()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithInline), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_Inline()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedInline), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_Failed_With_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithPrePlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Failed_With_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
           
        [Fact]
        public async Task Increase_Value_Failed_With_Inline_And_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithInlineAndPrePlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Failed_With_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }

        [Fact]
        public async Task Increase_Value_Failed_With_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithInlineAndPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Failed_Parallel_With_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedParallelWithInlineAndPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_Inline_And_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedInlineAndPrePlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_Inline_And_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedInlineAndPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedInlineAndPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Parallel_With_Failed_Inline_And_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueParallelWithFailedInlineAndPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 2,
                StringValue = "2"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_Failed_With_Inline_And_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueFailedWithInlineAndPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedPrePlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Increase_Value_With_Inline_And_Failed_PrePlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndFailedPrePlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_PrePlugin_And_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedPrePluginAndPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_Inline_And_Failed_PrePlugin_And_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndFailedPrePluginAndPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_Failed_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithFailedPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_Inline_And_Failed_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndFailedPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }
        
        [Fact]
        public async Task Increase_Value_With_PrePlugin_And_Failed_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithPrePluginAndFailedPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        [Fact]
        public async Task Increase_Value_With_Inline_And_PrePlugin_And_Failed_PostPlugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.IncreaseValueWithInlineAndPrePluginAndFailedPostPlugin), new IncreaseValueInput
                {
                    Key = key,
                    Memo = Guid.NewGuid().ToString()
                });
            var transactions = new List<Transaction> {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var messageValue = new MessageValue
            {
                Int64Value = 1,
                StringValue = "1"
            };
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, messageValue.StringValue, messageValue.Int64Value, messageValue);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(3);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, messageValue.Int64Value, messageValue.StringValue, messageValue);
        }
        
        #region private
        private async Task<Transaction> GenerateTransactionAsync(Address from,Address to,string methodName,IMessage input)
        {
            var transaction = _parallelTestHelper.GenerateTransaction(from, to, methodName, input);
            var transactionHash = transaction.GetHash();
            var signature = await _accountService.SignAsync(transactionHash.ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            var transactions = new List<Transaction> {transaction};
            await _parallelTestHelper.BroadcastTransactions(transactions);
            return transaction;
        }

        private async Task<GetValueOutput> GetValueAsync(Address from,string key,Hash blockHash,long blockHeight)
        {
            var byteString = await _parallelTestHelper.ExecuteReadOnlyAsync(new Transaction
            {
                From = from,
                To = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Params = new GetValueInput
                {
                    Key = key
                }.ToByteString(),
                MethodName = nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub.GetValue),

            }, blockHash, blockHeight);
            return GetValueOutput.Parser.ParseFrom(byteString);
        }

        private async Task SetIrreversibleBlockAsync(Chain chain)
        {
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
        }

        private void CheckValueNotExisted(GetValueOutput output)
        {
            output.StringValue.ShouldBeEmpty();
            output.Int64Value.ShouldBe(0);
            output.MessageValue.ShouldBeNull();
        }

        private async Task CheckValueNotExistedInVersionStateAsync(string key)
        {
            var state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"BoolValueMap", key}
                }
            }.ToStateKey());
            state.ShouldBeNull();
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"LongValueMap", key}
                }
            }.ToStateKey());
            state.ShouldBeNull();
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"StringValueMap", key}
                }
            }.ToStateKey());
            state.ShouldBeNull();
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"MessageValueMap", key}
                }
            }.ToStateKey());
            state.ShouldBeNull();
        }
        
        private async Task CheckValueInVersionStateAsync(string key,long longValue,string stringValue,MessageValue messageValue)
        {
            var state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"LongValueMap", key}
                }
            }.ToStateKey());
            SerializationHelper.Deserialize<long>(state.Value.ToByteArray())
                .ShouldBe(longValue);
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"StringValueMap", key}
                }
            }.ToStateKey());
            SerializationHelper.Deserialize<string>(state.Value.ToByteArray())
                .ShouldBe(stringValue);
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"MessageValueMap", key}
                }
            }.ToStateKey());
            SerializationHelper.Deserialize<MessageValue>(state.Value.ToByteArray())
                .ShouldBe(messageValue);
        }

        private void CheckValue(GetValueOutput output,string stringValue,long longValue,MessageValue messageValue)
        {
            output.StringValue.ShouldBe(stringValue);
            output.Int64Value.ShouldBe(longValue);
            output.MessageValue.ShouldBe(messageValue);
        }
        
        private async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId,BlockHeader blockHeader)
        {
            var transactionResult = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                blockHeader.GetHash());
            if (transactionResult != null) return transactionResult;
            return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                blockHeader.GetDisambiguatingHash());
        }

        private async Task<List<TransactionResult>> GetTransactionResultsAsync(List<Hash> transactionIds,BlockHeader blockHeader)
        {
            var transactionResults = new List<TransactionResult>();
            foreach (var transactionId in transactionIds)
            {
                var transactionResult = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    blockHeader.GetHash());
                if (transactionResult != null) transactionResults.Add(transactionResult);
                transactionResult = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    blockHeader.GetDisambiguatingHash());
                if(transactionResult != null) transactionResults.Add(transactionResult);
            }

            return transactionResults;
        }
        #endregion
    }
}