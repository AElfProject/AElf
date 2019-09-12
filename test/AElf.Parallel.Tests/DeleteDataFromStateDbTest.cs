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
using AElf.Sdk.CSharp.State;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Parallel.Tests
{
    public sealed class DeleteDataFromStateDbTest : AElfIntegratedTest<ParallelTestAElfModule>
    {
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBlockAttachService _blockAttachService;
        private readonly IAccountService _accountService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IStateStore<VersionedState> _versionedStates;
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;
        private readonly ITransactionGrouper _transactionGrouper;
        private readonly ICodeRemarksManager _codeRemarksManager;
        private readonly ParallelTestHelper _parallelTestHelper;

        public DeleteDataFromStateDbTest()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _accountService = GetRequiredService<IAccountService>();
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _versionedStates = GetRequiredService<IStateStore<VersionedState>>();
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
            _transactionGrouper = GetRequiredService<ITransactionGrouper>();
            _codeRemarksManager = GetRequiredService<ICodeRemarksManager>();
            _parallelTestHelper = GetRequiredService<ParallelTestHelper>();
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
                nameof(BasicFunctionWithParallelContract.RemoveValueParallel), new RemoveValueInput
                {
                    Key = key
                });
            var transactions = new[] {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(4);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Delete_After_Set_Key()
        {
            const string key = "TestKey";
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.RemoveAfterSetValue), new RemoveAfterSetValueInput
                {
                    Key = key,
                    BoolValue = true,
                    Int64Value = 10,
                    StringValue = "test",
                    MessageValue = new MessageValue
                    {
                        AddressValue = SampleAddress.AddressList[1],
                        BoolValue = true,
                        Int64Value = 20,
                        StringValue = "MessageTest",
                    }
                });
            var transactions = new[] {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(0);
            blockStateSet.Deletes.Count.ShouldBe(4);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(key);
        }

        [Fact]
        public async Task Set_After_Delete_Key()
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
                BoolValue = true,
                Int64Value = 10,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[1],
                    BoolValue = true,
                    Int64Value = 20,
                    StringValue = "MessageTest",
                }
            };
            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValue), setValueInput);
            var transactions = new[] {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, setValueInput.BoolValue, setValueInput.StringValue, setValueInput.Int64Value,
                setValueInput.MessageValue);
            
            var setAfterRemoveValueInput = new SetAfterRemoveValueInput
            {
                Key = key,
                BoolValue = false,
                Int64Value = 20,
                StringValue = "test2",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[1],
                    BoolValue = false,
                    Int64Value = 10,
                    StringValue = "MessageTest2",
                }
            };
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetAfterRemoveValue), setAfterRemoveValueInput);
            transactions = new[] {transaction};
            block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, setAfterRemoveValueInput.BoolValue, setAfterRemoveValueInput.StringValue,
                setAfterRemoveValueInput.Int64Value, setAfterRemoveValueInput.MessageValue);
            
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(4);
            blockStateSet.Deletes.Count.ShouldBe(0);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, setAfterRemoveValueInput.BoolValue,
                setAfterRemoveValueInput.Int64Value, setAfterRemoveValueInput.StringValue,
                setAfterRemoveValueInput.MessageValue);
        }
        
        [Fact]
        public async Task Set_With_Plugin()
        {
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);

            const string key = "TestKey";
            var prePluginKey = $"{key}_pre_plugin_key";
            var prePluginMessageValue = new MessageValue
            {
                AddressValue = SampleAddress.AddressList[1],
                BoolValue = true,
                Int64Value = 1,
                StringValue = $"{key}_pre_plugin_message_string"
            };

            var postPluginKey = $"{key}_post_plugin_key";
            var postPluginMessageValue = new MessageValue
            {
                AddressValue = SampleAddress.AddressList[3],
                BoolValue = true,
                Int64Value = 2,
                StringValue = $"{key}_post_plugin_message_string"
            };

            var prePluginKeyForDelete = $"{key}_pre_plugin_key_for_delete";
            
            var value = await GetValueAsync(accountAddress, key, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, prePluginKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, prePluginKeyForDelete, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, postPluginKey, chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);

            var setValueInput = new SetValueInput
            {
                Key = key,
                BoolValue = true,
                Int64Value = 10,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[1],
                    BoolValue = true,
                    Int64Value = 20,
                    StringValue = "MessageTest",
                }
            };

            var transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValueWithPlugin), setValueInput);
            var transactions = new[] {transaction};
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var transactionResult = await GetTransactionResultAsync(transaction.GetHash(), block.Header);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            value = await GetValueAsync(accountAddress, key, block.GetHash(), block.Height);
            CheckValue(value, setValueInput.BoolValue, setValueInput.StringValue, setValueInput.Int64Value,
                setValueInput.MessageValue);
            
            value = await GetValueAsync(accountAddress, prePluginKey, block.GetHash(), block.Height);
            CheckValue(value, true, $"{key}_pre_plugin_string", 1, prePluginMessageValue);
            
            value = await GetValueAsync(accountAddress, prePluginKeyForDelete, block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            
            value = await GetValueAsync(accountAddress, postPluginKey, block.GetHash(), block.Height);
            CheckValue(value, true, $"{key}_post_plugin_string", 2, postPluginMessageValue);
            
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(12);
            blockStateSet.Deletes.Count.ShouldBe(4);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueInVersionStateAsync(key, setValueInput.BoolValue, setValueInput.Int64Value,
                setValueInput.StringValue, setValueInput.MessageValue);
            await CheckValueNotExistedInVersionStateAsync(prePluginKeyForDelete);
            await CheckValueInVersionStateAsync(prePluginKey, true, 1, $"{key}_pre_plugin_string",
                prePluginMessageValue);
            await CheckValueInVersionStateAsync(postPluginKey, true, 2, $"{key}_post_plugin_string",
                postPluginMessageValue);
        }
        
        [Fact]
        public async Task Set_And_Delete_With_Parallel()
        {
            var keys = new[] {"TestKey1", "TestKey2", "TestKey3", "TestKey4"};
            var accountAddress = await _accountService.GetAccountAsync();
            var chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            
            var value = await GetValueAsync(accountAddress, keys[0], chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            value = await GetValueAsync(accountAddress, keys[1], chain.BestChainHash, chain.BestChainHeight);
            CheckValueNotExisted(value);
            
            var setValueInputs = new List<SetValueInput>();
            
            setValueInputs.Add(new SetValueInput
            {
                Key = keys[0],
                BoolValue = true,
                Int64Value = 30,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[0],
                    BoolValue = true,
                    Int64Value = 60,
                    StringValue = "MessageTest",
                }
            });
            
            var systemTransaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValueWithPlugin), setValueInputs[0]);

            var systemTransactions = new List<Transaction> {systemTransaction};
            setValueInputs.Add(new SetValueInput
            {
                Key = keys[1],
                BoolValue = true,
                Int64Value = 10,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[1],
                    BoolValue = true,
                    Int64Value = 20,
                    StringValue = "MessageTest",
                }
            });
            var transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValueWithPlugin), setValueInputs[1]);
            var transactions = new List<Transaction> {transaction};
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.RemoveValueWithPlugin), new RemoveValueInput
                {
                    Key = keys[1]
                });
            transactions.Add(transaction);
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.RemoveValueWithPlugin), new RemoveValueInput
                {
                    Key = keys[2]
                });
            transactions.Add(transaction);
            setValueInputs.Add(new SetValueInput
            {
                Key = keys[2],
                BoolValue = true,
                Int64Value = 20,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[2],
                    BoolValue = true,
                    Int64Value = 40,
                    StringValue = "MessageTest",
                }
            });
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValueWithPlugin), setValueInputs[2]);
            transactions.Add(transaction);
            
            transaction = await GenerateTransactionAsync(accountAddress, ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.RemoveValueWithPlugin), new RemoveValueInput
                {
                    Key = keys[0]
                });
            transactions.Add(transaction);
            
            setValueInputs.Add(new SetValueInput
            {
                Key = keys[3],
                BoolValue = true,
                Int64Value = 20,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[3],
                    BoolValue = true,
                    Int64Value = 40,
                    StringValue = "MessageTest",
                }
            });
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetValueWithPlugin), setValueInputs[3]);
            transactions.Add(transaction);
            
            var setAfterRemoveValueInput = new SetAfterRemoveValueInput
            {
                Key = keys[3],
                BoolValue = true,
                Int64Value = 60,
                StringValue = "test",
                MessageValue = new MessageValue
                {
                    AddressValue = SampleAddress.AddressList[4],
                    BoolValue = true,
                    Int64Value = 120,
                    StringValue = "MessageTest",
                }
            };
            transaction = await GenerateTransactionAsync(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.SetAfterRemoveValueWithPlugin), setAfterRemoveValueInput);
            transactions.Add(transaction);

            var groupedTransactions = await _transactionGrouper.GroupAsync(new ChainContext{BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight}, transactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(4);
            groupedTransactions.NonParallelizables.Count.ShouldBe(1);
            groupedTransactions.TransactionsWithoutContract.Count.ShouldBe(0);

            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,systemTransactions.Concat(transactions));
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions, transactions,
                CancellationToken.None);
            await _blockchainService.AddTransactionsAsync(transactions);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var codeRemarks =await _codeRemarksManager.GetCodeRemarksAsync(
                Hash.FromRawBytes(_parallelTestHelper.BasicFunctionWithParallelContractCode));
            codeRemarks.ShouldBeNull();
            
            value = await GetValueAsync(accountAddress, keys[0], block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            value = await GetValueAsync(accountAddress, keys[1], block.GetHash(), block.Height);
            CheckValueNotExisted(value);
            value = await GetValueAsync(accountAddress, keys[2], block.GetHash(), block.Height);
            CheckValue(value, setValueInputs[2].BoolValue, setValueInputs[2].StringValue, setValueInputs[2].Int64Value,
                setValueInputs[2].MessageValue);
            value = await GetValueAsync(accountAddress, keys[3], block.GetHash(), block.Height);
            CheckValue(value, setAfterRemoveValueInput.BoolValue, setAfterRemoveValueInput.StringValue, setAfterRemoveValueInput.Int64Value,
                setAfterRemoveValueInput.MessageValue);
            
            var blockStateSet = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
            blockStateSet.Changes.Count.ShouldBe(40);
            blockStateSet.Deletes.Count.ShouldBe(24);
            
            chain = await _blockchainService.GetChainAsync();
            await SetIrreversibleBlockAsync(chain);
            await CheckValueNotExistedInVersionStateAsync(keys[0]);
            await CheckValueNotExistedInVersionStateAsync(keys[1]);
            await CheckValueInVersionStateAsync(keys[2], setValueInputs[2].BoolValue,
                setValueInputs[2].Int64Value, setValueInputs[2].StringValue,
                setValueInputs[2].MessageValue);
            await CheckValueInVersionStateAsync(keys[3], setAfterRemoveValueInput.BoolValue,
                setAfterRemoveValueInput.Int64Value, setAfterRemoveValueInput.StringValue,
                setAfterRemoveValueInput.MessageValue);
            
        }

        #region private
        private async Task<Transaction> GenerateTransactionAsync(Address from,Address to,string methodName,IMessage input)
        {
            var transaction = _parallelTestHelper.GenerateTransaction(from, to, methodName, input);
            var transactionHash = transaction.GetHash();
            var signature = await _accountService.SignAsync(transactionHash.ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            //var transactions = new[] {transaction};
            //await _parallelTestHelper.BroadcastTransactions(transactions);
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
                MethodName = nameof(BasicFunctionWithParallelContract.GetValue),

            }, blockHash, blockHeight);
            return GetValueOutput.Parser.ParseFrom(byteString);
        }

        private async Task SetIrreversibleBlockAsync(Chain chain)
        {
            await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
            await _blockchainStateMergingService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
        }

        private void CheckValueNotExisted(GetValueOutput output)
        {
            output.BoolValue.ShouldBeFalse();
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
        
        private async Task CheckValueInVersionStateAsync(string key,bool boolValue,long longValue,string stringValue,MessageValue messageValue)
        {
            var state = await _versionedStates.GetAsync(new ScopedStatePath()
            {
                Address = ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                Path = new StatePath
                {
                    Parts = {"BoolValueMap", key}
                }
            }.ToStateKey());
            SerializationHelper.Deserialize<bool>(state.Value.ToByteArray())
                .ShouldBe(boolValue);
            
            state = await _versionedStates.GetAsync(new ScopedStatePath()
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

        private void CheckValue(GetValueOutput output,bool boolValue,string stringValue,long longValue,MessageValue messageValue)
        {
            output.BoolValue.ShouldBe(boolValue);
            output.StringValue.ShouldBe(stringValue);
            output.Int64Value.ShouldBe(longValue);
            output.MessageValue.ShouldBe(messageValue);
        }

        private async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, BlockHeader blockHeader)
        {
            var transactionResult = await _transactionResultManager.GetTransactionResultAsync(transactionId,
                blockHeader.GetHash());
            if (transactionResult != null) return transactionResult;
            return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                blockHeader.GetPreMiningHash());
        }
        #endregion
    }
}