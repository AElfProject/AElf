using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;
        private readonly IBlockTimeProvider _blockTimeProvider;

        private Address ConsensusContractAddress =>
            _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

        public MethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IChainContext chainContext, IBlockTimeProvider blockTimeProvider)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _chainContext = chainContext;
            _blockTimeProvider = blockTimeProvider;
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
                    From = Address.FromBytes(new byte[] { }.ComputeHash()),
                    To = ConsensusContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction,
                        _blockTimeProvider.GetBlockTime());

                return trace.IsSuccessful()
                    ? method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray())
                    : default;
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}