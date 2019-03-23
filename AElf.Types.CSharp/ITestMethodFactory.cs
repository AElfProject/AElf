using System;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Types.CSharp
{
    public interface IExecutionTask
    {
        Transaction Transaction { get; }
        TransactionResult TransactionResult { get; }
    }

    public interface IExecutionResult<out TOutput> : IExecutionTask where TOutput : IMessage<TOutput>
    {
        TOutput Output { get; }
    }

    public sealed class TestMethod<TInput, TOutput> where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
    {
        public Func<TInput, IExecutionResult<TOutput>> Send { get; }
        public Func<TInput, TOutput> Call { get; }

        public TestMethod(Func<TInput, IExecutionResult<TOutput>> send, Func<TInput, TOutput> call)
        {
            Send = send;
            Call = call;
        }
    }

    public interface ITestMethodFactory
    {
        TestMethod<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>;
    }
}