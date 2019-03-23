using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Common;
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
        public Address Self { get; set; }
        public Address Sender { get; set; }

        private readonly IRefBlockInfoProvider _refBlockInfoProvider;
        private readonly IAccountService _accountService;
        private readonly ITransactionExecutor _transactionExecutor;
        private readonly ITransactionResultService _transactionResultService;

        public TestMethodFactory(IRefBlockInfoProvider refBlockInfoProvider, IAccountService accountService,
            ITransactionExecutor transactionExecutor, ITransactionResultService transactionResultService)
        {
            _refBlockInfoProvider = refBlockInfoProvider;
            _accountService = accountService;
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
                    To = Self,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input)),
                    RefBlockNumber = refBlockInfo.Height,
                    RefBlockPrefix = refBlockInfo.Prefix
                };
                var signature = await _accountService.SignAsync(transaction.GetHashBytes());
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
                    To = Self,
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