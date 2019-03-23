using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestBase
{
    public class TestMethodFactory : ITestMethodFactory, ITransientDependency
    {
        public ECKeyPair KeyPair { get; set; } = CryptoHelpers.GenerateKeyPair();

        public Address ContractAddress { get; set; }

        public Address Sender => Address.FromPublicKey(KeyPair.PublicKey);

        private readonly IRefBlockInfoProvider _refBlockInfoProvider;
        private readonly ITransactionExecutor _transactionExecutor;
        private readonly ITransactionResultService _transactionResultService;

        public TestMethodFactory(IRefBlockInfoProvider refBlockInfoProvider, ITransactionExecutor transactionExecutor,
            ITransactionResultService transactionResultService)
        {
            _refBlockInfoProvider = refBlockInfoProvider;
            _transactionExecutor = transactionExecutor;
            _transactionResultService = transactionResultService;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public TestMethod<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
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
                var signature = CryptoHelpers.SignWithPrivateKey(
                    KeyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
                transaction.Sigs.Add(ByteString.CopyFrom(signature));
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

            return new TestMethod<TInput, TOutput>(SendAsync, CallAsync);
        }
    }
}