using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    internal interface IParliamentContractReaderFactory
    {
        ParliamentContractContainer.ParliamentContractStub Create(Hash blockHash, long blockHeight,
            Address sender = null);
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

        private ParliamentContractContainer.ParliamentContractStub Create(Address sender,
            IChainContext chainContext)
        {
            return new ParliamentContractContainer.ParliamentContractStub()
            {
                __factory = new MethodStubFactory(_transactionReadOnlyExecutionService, _smartContractAddressService,
                    chainContext, sender)
            };
        }

        public ParliamentContractContainer.ParliamentContractStub Create(Hash blockHash, long blockHeight,
            Address sender = null)
        {
            return Create(sender, new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            });
        }
    }
    
    internal class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private Address ParliamentContractMethodAddress =>
            _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider.Name);

        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IChainContext _chainContext;

        private Address FromAddress { get; }
        
        public MethodStubFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ISmartContractAddressService smartContractAddressService, IChainContext chainContext, Address sender)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
            _chainContext = chainContext;
            FromAddress = sender ?? Address.FromBytes(new byte[] { }.ComputeHash());
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