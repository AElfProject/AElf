using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase.ContractTestKit
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public ECKeyPair KeyPair { get; set; } = CryptoHelper.GenerateKeyPair();

        public Address ContractAddress { get; set; }

        public Address Sender => Address.FromPublicKey(KeyPair.PublicKey);

        private readonly IRefBlockInfoProvider _refBlockInfoProvider;
        private readonly ITestTransactionExecutor _testTransactionExecutor;
        private readonly IBlockTimeProvider _blockTimeProvider;
        private readonly IResetBlockTimeProvider _resetBlockTimeProvider;

        public MethodStubFactory(IServiceProvider serviceProvider)
        {
            _refBlockInfoProvider = serviceProvider.GetRequiredService<IRefBlockInfoProvider>();
            _testTransactionExecutor = serviceProvider.GetRequiredService<ITestTransactionExecutor>();
            _blockTimeProvider = serviceProvider.GetRequiredService<IBlockTimeProvider>();
            _resetBlockTimeProvider = serviceProvider.GetRequiredService<IResetBlockTimeProvider>();
        }

        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            Transaction GetTransaction(TInput input)
            {
                var refBlockInfo = _refBlockInfoProvider.GetRefBlockInfo();
                var transaction = GetTransactionWithoutSignature(input, method);
                transaction.RefBlockNumber = refBlockInfo.Height;
                transaction.RefBlockPrefix = refBlockInfo.Prefix;

                var signature = CryptoHelper.SignWithPrivateKey(
                    KeyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);
                return transaction;
            }

            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var transaction = GetTransaction(input);
                var transactionResult = await _testTransactionExecutor.ExecuteAsync(transaction);
                
                if (transactionResult == null)
                {
                    return new ExecutionResult<TOutput> {Transaction = transaction};
                }

                ResetBlockTime();
                return new ExecutionResult<TOutput>
                {
                    Transaction = transaction, TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(transactionResult.ReturnValue.ToByteArray())
                };
            }

            async Task<IExecutionResult<TOutput>> SendWithExceptionAsync(TInput input)
            {
                var transaction = GetTransaction(input);
                var transactionResult =await _testTransactionExecutor.ExecuteWithExceptionAsync(transaction);
                if (transactionResult == null)
                {
                    return new ExecutionResult<TOutput> {Transaction = transaction};
                }

                ResetBlockTime();
                return new ExecutionResult<TOutput>
                {
                    Transaction = transaction, TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(transactionResult.ReturnValue.ToByteArray())
                };
            }

            void ResetBlockTime()
            {
                if (!_resetBlockTimeProvider.Enabled) return;
                var currentBlockTime = _blockTimeProvider.GetBlockTime();
                currentBlockTime.AddMilliseconds(_resetBlockTimeProvider.StepMilliseconds);
                _blockTimeProvider.SetBlockTime(currentBlockTime);
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = GetTransactionWithoutSignature(input, method);
                var returnValue = await _testTransactionExecutor.ReadAsync(transaction);
                return method.ResponseMarshaller.Deserializer(returnValue.ToByteArray());
            }

            async Task<StringValue> CallWithExceptionAsync(TInput input)
            {
                var transaction = GetTransactionWithoutSignature(input, method);
                var returnValue = await _testTransactionExecutor.ReadWithExceptionAsync(transaction);
                return new StringValue {Value = returnValue.Value};
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync, GetTransaction, SendWithExceptionAsync,
                CallWithExceptionAsync);
        }

        private Transaction GetTransactionWithoutSignature<TInput, TOutput>(TInput input,
            Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            var transaction = new Transaction
            {
                From = Sender,
                To = ContractAddress,
                MethodName = method.Name,
                Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
            };

            return transaction;
        }
    }
}