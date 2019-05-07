using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private Address CrossChainContractMethodAddress =>
            _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);

        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;

        public MethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IChainContext chainContext)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _chainContext = chainContext;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var chainContext = _chainContext;
                var transaction = new Transaction()
                {
                    From = Address.Zero,
                    To = CrossChainContractMethodAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction, DateTime.UtcNow);

                if (trace.IsSuccessful())
                {
                    return method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray());
                }

                trace.SurfaceUpError();
                if (string.IsNullOrEmpty(trace.StdErr))
                {
                    return default(TOutput);
                }
                throw new Exception(trace.StdErr);
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}