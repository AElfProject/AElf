﻿using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.CSharp.Core
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

    public interface IMethodStub<TInput, TOutput> where TInput : IMessage<TInput> where TOutput : IMessage<TOutput>
    {
        Method<TInput, TOutput> Method { get; }
        Func<TInput, Task<IExecutionResult<TOutput>>> SendAsync { get; }
        Func<TInput, Task<TOutput>> CallAsync { get; }
        Func<TInput, Transaction> GetTransaction { get; }
    }

    public sealed class MethodStub<TInput, TOutput> : IMethodStub<TInput, TOutput> where TInput : IMessage<TInput>
        where TOutput : IMessage<TOutput>
    {
        public Method<TInput, TOutput> Method { get; }
        public Func<TInput, Task<IExecutionResult<TOutput>>> SendAsync { get; }
        public Func<TInput, Task<TOutput>> CallAsync { get; }
        public Func<TInput, Transaction> GetTransaction { get; }

        public MethodStub(Method<TInput, TOutput> method, Func<TInput, Task<IExecutionResult<TOutput>>> sendAsync,
            Func<TInput, Task<TOutput>> callAsync, Func<TInput, Transaction> getTransaction = null)
        {
            Method = method;
            SendAsync = sendAsync;
            CallAsync = callAsync;
            GetTransaction = getTransaction;
        }
    }

    public interface IMethodStubFactory
    {
        IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new();
    }
}