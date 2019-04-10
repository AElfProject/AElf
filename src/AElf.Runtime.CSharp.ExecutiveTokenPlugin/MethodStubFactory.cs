using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    public class MethodStubFactory : IMethodStubFactory
    {
        private readonly IHostSmartContractBridgeContext _context;

        public MethodStubFactory(IHostSmartContractBridgeContext context)
        {
            _context = context;
        }

        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            var context = _context;

            async Task<TOutput> CallAsync(TInput input)
            {
                return _context.Call<TOutput>(context.StateProvider.Cache, context.Self, method.Name,
                    input.ToByteString());
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}