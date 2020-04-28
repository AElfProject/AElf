using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public class ReadOnlyMethodStubFactory : IMethodStubFactory
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private ContractReaderContext _contractReaderContext;

        public ReadOnlyMethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                if (_contractReaderContext.ContractAddress == null)
                    return default;

                var chainContext = new ChainContext
                {
                    BlockHash = _contractReaderContext.BlockHash,
                    BlockHeight = _contractReaderContext.BlockHeight,
                    StateCache = _contractReaderContext.StateCache
                };
                var transaction = new Transaction()
                {
                    From = _contractReaderContext.Sender ?? Address.FromBytes(new byte[] { }.ComputeHash()),
                    To = _contractReaderContext.ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                        _contractReaderContext.Timestamp ?? TimestampHelper.GetUtcNow());

                return trace.IsSuccessful()
                    ? method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray())
                    : default;
            }

            Transaction GetTransaction(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = _contractReaderContext.Sender,
                    To = _contractReaderContext.ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                return transaction;
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync, GetTransaction);
        }

        public void SetContractReaderContext(ContractReaderContext contractReaderContext)
        {
            _contractReaderContext = contractReaderContext;
        }

    }
}