using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.CSharp.Core;
using Google.Protobuf;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Runtime.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Assembly _contractAssembly;
        private readonly object _contractInstance;
        private readonly ReadOnlyDictionary<string, IServerCallHandler> _callHandlers;
        private readonly ServerServiceDefinition _serverServiceDefinition;

        private CSharpSmartContractProxy _smartContractProxy;
        private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

        private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
        public IReadOnlyList<ServiceDescriptor> Descriptors { get; }

        public bool IsSystemContract { get; set; }
        public Timestamp LastUsedTime { get; set; }

        private ServerServiceDefinition GetServerServiceDefinition(Assembly assembly)
        {
            var methodInfo = assembly.FindContractContainer().GetMethod("BindService",
                new[] {assembly.FindContractBaseType()});
            return methodInfo.Invoke(null, new[] {_contractInstance}) as ServerServiceDefinition;
        }

        public Executive(Assembly assembly)
        {
            _contractAssembly = assembly;
            _contractInstance = Activator.CreateInstance(assembly.FindContractType());
            _smartContractProxy = new CSharpSmartContractProxy(_contractInstance, assembly.FindExecutionObserverType());
            _serverServiceDefinition = GetServerServiceDefinition(assembly);
            _callHandlers = _serverServiceDefinition.GetCallHandlers();
            Descriptors = _serverServiceDefinition.GetDescriptors();
        }

        public IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext)
        {
            _hostSmartContractBridgeContext = smartContractBridgeContext;
            _smartContractProxy.InternalInitialize(_hostSmartContractBridgeContext);
            return this;
        }

        private void Cleanup()
        {
            _smartContractProxy.Cleanup();
        }

        public Task ApplyAsync(ITransactionContext transactionContext)
        {
            try
            {
                _hostSmartContractBridgeContext.TransactionContext = transactionContext;
                if (CurrentTransactionContext.CallDepth > CurrentTransactionContext.MaxCallDepth)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                    CurrentTransactionContext.Trace.Error = "\n" + "ExceededMaxCallDepth";
                    return Task.CompletedTask;
                }

                Execute();
            }
            finally
            {
                _hostSmartContractBridgeContext.TransactionContext = null;
            }

            return Task.CompletedTask;
        }

        public void Execute()
        {
            var s = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();
            var methodName = CurrentTransactionContext.Transaction.MethodName;
            var observer = IsSystemContract ? 
                new ExecutionObserver(-1, -1) : // Counters are active but no threshold
                new ExecutionObserver(CurrentTransactionContext.ExecutionCallThreshold, 
                    CurrentTransactionContext.ExecutionBranchThreshold);
            
            try
            {
                if (!_callHandlers.TryGetValue(methodName, out var handler))
                {
                    throw new RuntimeException(
                        $"Failed to find handler for {methodName}. We have {_callHandlers.Count} handlers: " +
                        string.Join(", ", _callHandlers.Keys.OrderBy(k => k))
                    );
                }
                
                _smartContractProxy.SetExecutionObserver(observer);
                
                ExecuteTransaction(handler);

                if (!handler.IsView())
                {
                    CurrentTransactionContext.Trace.StateSet = GetChanges();
                }
                else
                {
                    CurrentTransactionContext.Trace.StateSet = new TransactionExecutingStateSet();
                }
            }
            catch (Exception ex)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
                CurrentTransactionContext.Trace.Error += ex + "\n";
            }
            finally
            {
                CurrentTransactionContext.Trace.ExecutionCallCount = observer.GetCallCount();
                CurrentTransactionContext.Trace.ExecutionBranchCount = observer.GetBranchCount();
                Cleanup();
            }

            var e = CurrentTransactionContext.Trace.EndTime = TimestampHelper.GetUtcNow().ToDateTime();
            CurrentTransactionContext.Trace.Elapsed = (e - s).Ticks;
        }

        public string GetJsonStringOfParameters(string methodName, byte[] paramsBytes)
        {
            if (!_callHandlers.TryGetValue(methodName, out var handler))
            {
                return "";
            }

            return handler.InputBytesToString(paramsBytes);
        }

        private IEnumerable<FileDescriptor> GetSelfAndDependency(FileDescriptor fileDescriptor,
            HashSet<string> known = null)
        {
            known = known ?? new HashSet<string>();
            if (known.Contains(fileDescriptor.Name))
            {
                return new List<FileDescriptor>();
            }

            var fileDescriptors = new List<FileDescriptor>();
            fileDescriptors.AddRange(fileDescriptor.Dependencies.SelectMany(x => GetSelfAndDependency(x, known)));
            fileDescriptors.Add(fileDescriptor);
            known.Add(fileDescriptor.Name);
            return fileDescriptors;
        }

        public byte[] GetFileDescriptorSet()
        {
            var descriptor = Descriptors.Last();
            var output = new FileDescriptorSet();
            output.File.AddRange(GetSelfAndDependency(descriptor.File).Select(x => x.SerializedData));
            return output.ToByteArray();
        }

        public IEnumerable<FileDescriptor> GetFileDescriptors()
        {
            var descriptor = Descriptors.Last();
            return GetSelfAndDependency(descriptor.File);
        }

        public Hash ContractHash { get; set; }

        private void ExecuteTransaction(IServerCallHandler handler)
        {
            try
            {
                var tx = CurrentTransactionContext.Transaction;
                var retVal = handler.Execute(tx.Params.ToByteArray());
                if (retVal != null)
                {
                    CurrentTransactionContext.Trace.ReturnValue = ByteString.CopyFrom(retVal);
                    CurrentTransactionContext.Trace.ReadableReturnValue = handler.ReturnBytesToString(retVal);
                }

                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
            }
            catch (TargetInvocationException ex)
            {
                CurrentTransactionContext.Trace.Error += ex;
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
            }
            catch (Exception ex)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                CurrentTransactionContext.Trace.Error += "\n" + ex;
            }
        }
        
        private TransactionExecutingStateSet GetChanges()
        {
            var changes = _smartContractProxy.GetChanges();

            var address = _hostSmartContractBridgeContext.Self.ToStorageKey();
            foreach (var key in changes.Writes.Keys)
            {
                if (!key.StartsWith(address))
                {
                    throw new InvalidOperationException("a contract cannot access other contracts data");
                }
            }
                    
            foreach (var (key, value) in changes.Deletes)
            {
                if (!key.StartsWith(address))
                {
                    throw new InvalidOperationException("a contract cannot access other contracts data");
                }
            }

            foreach (var key in changes.Reads.Keys)
            {
                if (!key.StartsWith(address))
                {
                    throw new InvalidOperationException("a contract cannot access other contracts data");
                }
            }

            if (!CurrentTransactionContext.Trace.IsSuccessful())
            {
                changes.Writes.Clear();
                changes.Deletes.Clear();
            }

            return changes;
        }
    }
}