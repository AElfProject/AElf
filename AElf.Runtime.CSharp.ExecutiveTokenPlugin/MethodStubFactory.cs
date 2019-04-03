using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    public class MethodStubFactory:IMethodStubFactory
    {
        private readonly Func<Transaction, TransactionTrace> _readOnlyExecutor;
        private readonly Address _selfAddress;

        public MethodStubFactory(Func<Transaction, TransactionTrace> readOnlyExecutor, Address selfAddress)
        {
            _readOnlyExecutor = readOnlyExecutor;
            _selfAddress = selfAddress;
        }
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method) where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = Address.Zero,
                    To = _selfAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                var trace = _readOnlyExecutor(transaction);
                var returnValue = trace.ReturnValue;
                return method.ResponseMarshaller.Deserializer(returnValue.ToByteArray());
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}