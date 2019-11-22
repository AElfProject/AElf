using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.Application
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;
        private readonly IConsensusReaderContextService _contextService;

        private Address FromAddress { get; }

        private Address ConsensusContractAddress =>
            _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

        public MethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IChainContext chainContext, 
            IConsensusReaderContextService contextService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _chainContext = chainContext;
            _contextService = contextService;

            FromAddress = AsyncHelper.RunSync(() => _contextService.GetAccountAsync());
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
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
                    To = ConsensusContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                        _contextService.GetBlockTime());

                return trace.IsSuccessful()
                    ? method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray())
                    : default;
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}