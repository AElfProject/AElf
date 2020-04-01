using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    //TODO: base class
    public class ZeroContractMethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        private Address ZeroContractAddress => _smartContractAddressService.GetZeroSmartContractAddress();

        public ZeroContractMethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IChainContext chainContext)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _chainContext = chainContext;
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
                var chainContext = _chainContext;
                var transaction = new Transaction()
                {
                    From = FromAddress,
                    To = ZeroContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                        TimestampHelper.GetUtcNow());

                return trace.IsSuccessful()
                    ? method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray())
                    : default;
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}