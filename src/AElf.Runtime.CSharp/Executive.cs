using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.CSharp.Core;
using Google.Protobuf;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Reflection;

namespace AElf.Runtime.CSharp
{
    public class Executive : IExecutive
    {
        private Assembly _contractAssembly;
        private Type _contractType;
        private object _contractInstance;
        private ReadOnlyDictionary<string, IServerCallHandler> _callHandlers;
        private ServerServiceDefinition _serverServiceDefinition;

        private CSharpSmartContractProxy _smartContractProxy;
        private ITransactionContext CurrentTransactionContext => _hostSmartContractBridgeContext.TransactionContext;

        private IHostSmartContractBridgeContext _hostSmartContractBridgeContext;
        private IServiceContainer<IExecutivePlugin> _executivePlugins;
        public IReadOnlyList<ServiceDescriptor> Descriptors => _descriptors;
        private List<ServiceDescriptor> _descriptors;

        private AssemblyLoadContext _acl;

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

        public Executive(IServiceContainer<IExecutivePlugin> executivePlugins)
        {
            _executivePlugins = executivePlugins;
        }

        public void Load(byte[] code, AssemblyLoadContext loadContext)
        {
            _acl = loadContext;

            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = _acl.LoadFromStream(stream);
            }

            if (assembly == null)
            {
                throw new InvalidCodeException("Invalid binary code.");
            }
            
            _contractAssembly = assembly;
            _contractType = FindContractType(assembly);
            _contractInstance = Activator.CreateInstance(_contractType);
            _smartContractProxy = new CSharpSmartContractProxy(_contractInstance);
            _serverServiceDefinition = GetServerServiceDefinition(assembly);
            _callHandlers = _serverServiceDefinition.GetCallHandlers();
            _descriptors = _serverServiceDefinition.GetDescriptors().ToList();
        }

        public void Unload()
        {
            var acl = _acl;
            _acl = null;
            
            _contractAssembly = null;
            _contractType = null;
            _contractInstance = null;
            _smartContractProxy = null;
            _serverServiceDefinition = null;
            _callHandlers = null;
            _descriptors = null;

            acl.Unload();
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
                    CurrentTransactionContext.Trace.Error = "\n" + "ExceededMaxCallDepth";
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
            var s = CurrentTransactionContext.Trace.StartTime = TimestampHelper.GetUtcNow().ToDateTime();
            var methodName = CurrentTransactionContext.Transaction.MethodName;

            try
            {
                if (!_callHandlers.TryGetValue(methodName, out var handler))
                {
                    throw new RuntimeException(
                        $"Failed to find handler for {methodName}. We have {_callHandlers.Count} handlers: " +
                        string.Join(", ", _callHandlers.Keys.OrderBy(k => k))
                    );
                }

                try
                {
                    var tx = CurrentTransactionContext.Transaction;
                    var retVal = handler.Execute(tx.Params.ToByteArray());
                    if (retVal != null)
                    {
                        CurrentTransactionContext.Trace.ReturnValue = ByteString.CopyFrom(retVal);
                        // TODO: Clean up ReadableReturnValue
                        CurrentTransactionContext.Trace.ReadableReturnValue = handler.ReturnBytesToString(retVal);
                    }

                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.Executed;
                }
                catch (TargetInvocationException ex)
                {
                    CurrentTransactionContext.Trace.Error += ex;
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                }
                catch (AssertionException ex)
                {
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    CurrentTransactionContext.Trace.Error += "\n" + ex;
                }
                catch (Exception ex)
                {
                    // TODO: Simplify exception
                    CurrentTransactionContext.Trace.ExecutionStatus = ExecutionStatus.ContractError;
                    CurrentTransactionContext.Trace.Error += "\n" + ex;
                }

                if (!handler.IsView())
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
                CurrentTransactionContext.Trace.Error += ex + "\n";
            }
            finally
            {
                // TODO: Not needed
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
    }
}