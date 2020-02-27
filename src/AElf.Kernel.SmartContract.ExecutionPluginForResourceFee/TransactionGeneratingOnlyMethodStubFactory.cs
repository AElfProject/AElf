using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class TransactionGeneratingOnlyMethodStubFactory : IMethodStubFactory
    {
        public Address Sender { get; set; }
        public Address ContractAddress { get; set; }

        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                return Task.FromResult<IExecutionResult<TOutput>>(new ExecutionResult<TOutput>
                {
                    Transaction = new Transaction
                    {
                        From = Sender,
                        To = ContractAddress,
                        MethodName = method.Name,
                        Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                    }
                });
            }

            Task<TOutput> CallAsync(TInput input)
            {
                throw new NotSupportedException();
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}