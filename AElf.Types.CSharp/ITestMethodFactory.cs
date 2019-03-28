using System;
using System.Threading.Tasks;
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

    public class ExecutionTask : IExecutionTask
    {
        public Transaction Transaction { get; set; }
        public TransactionResult TransactionResult { get; set; }
    }

    public class ExecutionResult<TOutput> : ExecutionTask, IExecutionResult<TOutput> where TOutput : IMessage<TOutput>
    {
        public TOutput Output { get; set; }
    }

    public sealed class TestMethod<TInput, TOutput> where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
    {
        public TestMethod(Func<TInput, Task<IExecutionResult<TOutput>>> sendAsync,
            Func<TInput, Task<TOutput>> callAsync)
        {
            SendAsync = sendAsync;
            CallAsync = callAsync;
        }

        public Func<TInput, Task<IExecutionResult<TOutput>>> SendAsync { get; }
        public Func<TInput, Task<TOutput>> CallAsync { get; }
    }

    public interface ITestMethodFactory
    {
        TestMethod<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>;
    }
}