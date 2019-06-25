using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using Google.Protobuf;
using System.Collections.Generic;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
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
                return _context.Call<TOutput>(context.Self, method.Name,
                    input.ToByteString());
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
        
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(List<Method<TInput, TOutput>> methods)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotImplementedException();
            }

            async Task<List<TOutput>> CallAsync(TInput input)
            {
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }
    }
}