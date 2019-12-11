using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Contracts.ParliamentAuth;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    internal interface IParliamentContractReaderFactory
    {
        ParliamentAuthContractContainer.ParliamentAuthContractStub Create(Hash blockHash, long blockHeight);
    }

    internal class ParliamentContractReaderFactory : IParliamentContractReaderFactory, ITransientDependency
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ParliamentContractReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
        }

        private ParliamentAuthContractContainer.ParliamentAuthContractStub Create(IChainContext chainContext)
        {
            return new ParliamentAuthContractContainer.ParliamentAuthContractStub()
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService, _smartContractAddressService,
                    chainContext)
            };
        }

        public ParliamentAuthContractContainer.ParliamentAuthContractStub Create(Hash blockHash, long blockHeight)
        {
            return Create(new ChainContext()
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
    
    internal class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private Address ParliamentContractMethodAddress =>
            _smartContractAddressService.GetAddressByContractName(ParliamentAuthSmartContractAddressNameProvider.Name);

        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());
        
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
            Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                if (ParliamentContractMethodAddress == null)
                    return default;
                
                var chainContext = _chainContext;
                var transaction = new Transaction()
                {
                    From = FromAddress,
                    To = ParliamentContractMethodAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };

                var trace =
                    await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, transaction, TimestampHelper.GetUtcNow());

                return trace.IsSuccessful() ? method.ResponseMarshaller.Deserializer(trace.ReturnValue.ToByteArray()) : default;
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}