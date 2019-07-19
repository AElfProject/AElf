using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public ECKeyPair KeyPair { get; set; } = CryptoHelper.GenerateKeyPair();

        public Address ContractAddress { get; set; }

        public Address Sender => Address.FromPublicKey(KeyPair.PublicKey);

        private readonly IRefBlockInfoProvider _refBlockInfoProvider;
        private readonly ITransactionExecutor _transactionExecutor;
        private readonly ITransactionResultService _transactionResultService;

        public MethodStubFactory(IServiceProvider serviceProvider)
        {
            _refBlockInfoProvider = serviceProvider.GetRequiredService<IRefBlockInfoProvider>();
            _transactionExecutor = serviceProvider.GetRequiredService<ITransactionExecutor>();
            _transactionResultService = serviceProvider.GetRequiredService<ITransactionResultService>();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var refBlockInfo = _refBlockInfoProvider.GetRefBlockInfo();
                var transaction = new Transaction()
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input)),
                    RefBlockNumber = refBlockInfo.Height,
                    RefBlockPrefix = refBlockInfo.Prefix
                };
                var signature = CryptoHelper.SignWithPrivateKey(
                    KeyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);
                await _transactionExecutor.ExecuteAsync(transaction);
                var transactionResult =
                    await _transactionResultService.GetTransactionResultAsync(transaction.GetHash());
                return new ExecutionResult<TOutput>()
                {
                    Transaction = transaction, TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(transactionResult.ReturnValue.ToByteArray())
                };
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                var returnValue = await _transactionExecutor.ReadAsync(transaction);
                return method.ResponseMarshaller.Deserializer(returnValue.ToByteArray());
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}