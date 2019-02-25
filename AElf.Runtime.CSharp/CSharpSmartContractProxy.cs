using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Runtime.CSharp
{
    public class CSharpSmartContractProxy
    {
        private static MethodInfo GetMethedInfo(Type type, string name)
        {
            return type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        }

        private object _instance;

        private Dictionary<string, MethodInfo> _methodInfos = new Dictionary<string, MethodInfo>();

        public CSharpSmartContractProxy(object instance)
        {
            _instance = instance;
            InitializeMethodInfos(_instance.GetType());
        }

        private void InitializeMethodInfos(Type instanceType)
        {
            _methodInfos = new[]
            {
                nameof(SetSmartContractContext), nameof(SetTransactionContext), nameof(SetStateProvider),
                nameof(GetChanges), nameof(Cleanup)
            }.ToDictionary(x => x, x => GetMethedInfo(instanceType, x));
        }

        public void SetSmartContractContext(ISmartContractContext smartContractContext)
        {
            _methodInfos[nameof(SetSmartContractContext)].Invoke(_instance, new object[] {smartContractContext});
        }

        public void SetTransactionContext(ITransactionContext transactionContext)
        {
            _methodInfos[nameof(SetTransactionContext)].Invoke(_instance, new object[] {transactionContext});
        }

        public void SetStateProvider(IStateProvider stateProvider)
        {
            _methodInfos[nameof(SetStateProvider)].Invoke(_instance, new object[] {stateProvider});
        }

        public Dictionary<StatePath, StateValue> GetChanges()
        {
            return (Dictionary<StatePath, StateValue>) _methodInfos[nameof(GetChanges)]
                .Invoke(_instance, new object[0]);
        }

        internal void Cleanup()
        {
            _methodInfos[nameof(Cleanup)].Invoke(_instance, new object[0]);
        }
    }
}