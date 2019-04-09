using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Types.CSharp;
using Google.Protobuf;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Sdk.CSharp;
using Google.Protobuf.Reflection;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private readonly Assembly _contractAssembly;
        private readonly Type _contractType;
        private readonly object _contractInstance;
        private readonly ReadOnlyDictionary<string, IServerCallHandler> _callHandlers;
        private readonly IReadOnlyList<ServiceDescriptor> _descriptors;
        private readonly ServerServiceDefinition _serverServiceDefinition;

        private CSharpSmartContractProxy _smartContractProxy;
        private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

        private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
        private readonly IServiceContainer<IExecutivePlugin> _executivePlugins;

        private Type FindContractType(Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && !t.IsNested);
        }

        private Type FindContractBaseType(Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && t.IsNested);
        }

        private Type FindContractContainer(Assembly assembly)
        {
            var contractBase = FindContractBaseType(assembly);
            return contractBase.DeclaringType;
        }

        private ServerServiceDefinition GetServerServiceDefinition(Assembly assembly)
        {
            var methodInfo = FindContractContainer(assembly).GetMethod("BindService",
                new[] {FindContractBaseType(assembly)});
            return methodInfo.Invoke(null, new[] {_contractInstance}) as ServerServiceDefinition;
        }

        public Executive(Assembly assembly, IServiceContainer<IExecutivePlugin> executivePlugins)
        {
            _contractAssembly = assembly;
            _executivePlugins = executivePlugins;
            _contractType = FindContractType(assembly);
            _contractInstance = Activator.CreateInstance(_contractType);
            _smartContractProxy = new CSharpSmartContractProxy(_contractInstance);
            _serverServiceDefinition = GetServerServiceDefinition(assembly);
            _callHandlers = _serverServiceDefinition.GetCallHandlers();
            _descriptors = _serverServiceDefinition.GetDescriptors();
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

        public async Task ApplyAsync(ITransactionContext transactionContext)
        {
            try
            {
                _hostSmartContractBridgeContext.TransactionContext = transactionContext;
                if (CurrentTransactionContext.CallDepth > CurrentTransactionContext.MaxCallDepth)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth;
                    CurrentTransactionContext.Trace.StdErr = "\n" + "ExceededMaxCallDepth";
                    return;
                }

                Execute();
                if (CurrentTransactionContext.CallDepth == 0)
                {
                    // Plugin should only apply to top level transaction
                    foreach (var plugin in _executivePlugins)
                    {
                        plugin.PostMain(_hostSmartContractBridgeContext, _serverServiceDefinition);
                    }
                }
            }
            finally
            {
                _hostSmartContractBridgeContext.TransactionContext = null;
            }
        }

        public void Execute()
        {
            var s = CurrentTransactionContext.Trace.StartTime = DateTime.UtcNow;
            var methodName = CurrentTransactionContext.Transaction.MethodName;

            try
            {
                if (!_callHandlers.TryGetValue(methodName, out var handler))
                {
                    throw new RuntimeException(
                        $"Failed to find handler for {methodName}. We have {_callHandlers.Count} handlers.");
                }

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
                    CurrentTransactionContext.Trace.StdErr += ex.InnerException;
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                }
                catch (AssertionException ex)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    CurrentTransactionContext.Trace.StdErr += "\n" + ex;
                }
                catch (Exception ex)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    CurrentTransactionContext.Trace.StdErr += "\n" + ex;
                }

                if (!handler.IsView() && CurrentTransactionContext.Trace.IsSuccessful())
                {
                    var changes = _smartContractProxy.GetChanges();

                    var address = _hostSmartContractBridgeContext.Self.ToStorageKey();
                    foreach (var (key, value) in changes.Writes)
                    {
                        if (!key.StartsWith(address))
                        {
                            throw new InvalidOperationException("a contract cannot access other contracts data");
                        }
                    }

                    foreach (var (key, value) in changes.Reads)
                    {
                        if (!key.StartsWith(address))
                        {
                            throw new InvalidOperationException("a contract cannot access other contracts data");
                        }
                    }


                    CurrentTransactionContext.Trace.StateSet = changes;
                }
                else
                {
                    CurrentTransactionContext.Trace.StateSet = new TransactionExecutingStateSet();
                }
            }
            catch (Exception ex)
            {
                CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.SystemError;
                CurrentTransactionContext.Trace.StdErr += ex + "\n";
            }
            finally
            {
                Cleanup();
            }

            var e = CurrentTransactionContext.Trace.EndTime = DateTime.UtcNow;
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

//        public object GetReturnValue(string methodName, byte[] bytes)
//        {
//            if (!_callHandlers.TryGetValue(methodName, out var handler))
//            {
//                return null;
//            }
//
//            return handler.ReturnBytesToObject(bytes);
//        }

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
            var descriptor = _descriptors.Last();
            var output = new FileDescriptorSet();
            output.File.AddRange(GetSelfAndDependency(descriptor.File).Select(x => x.SerializedData));
            return output.ToByteArray();
        }
    }
}