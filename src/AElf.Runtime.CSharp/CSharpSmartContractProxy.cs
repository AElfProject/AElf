using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AElf.CSharp.CodeOps;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Runtime.CSharp
{
    public class CSharpSmartContractProxy
    {
        private static MethodInfo GetMethodInfo(Type type, string name)
        {
            return type.GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        }


        private static Delegate CreateDelegate(object instance, MethodInfo method)
        {
            return Delegate.CreateDelegate
            (
                Expression.GetDelegateType
                (
                    method.GetParameters()
                        .Select(p => p.ParameterType)
                        .Concat(new Type[] {method.ReturnType})
                        .ToArray()
                ),
                instance,
                method
            );
        }

        private static Delegate CreateDelegate(object instance, Type type, string name)
        {
            var methodInfo = GetMethodInfo(type, name);
            return methodInfo == null ? null : CreateDelegate(instance, methodInfo);
        }

        private readonly Action _methodCleanup;
        private readonly Func<TransactionExecutingStateSet> _methodGetChanges;
        private readonly Action<ISmartContractBridgeContext> _methodInternalInitialize;


        private readonly Action _methodResetFields; // can be null
        private readonly Action<IExecutionObserver> _methodSetExecutionObserver; // can be null

        public CSharpSmartContractProxy(object instance, Type counterType)
        {
            var instanceType = instance.GetType();

            _methodCleanup = (Action) CreateDelegate(instance, instanceType, nameof(Cleanup));

            _methodGetChanges =
                (Func<TransactionExecutingStateSet>) CreateDelegate(instance, instanceType, nameof(GetChanges));

            _methodInternalInitialize =
                (Action<ISmartContractBridgeContext>) CreateDelegate(instance, instanceType,
                    nameof(InternalInitialize));

            _methodResetFields = (Action) CreateDelegate(instance, instanceType, nameof(ResetFields));

            _methodSetExecutionObserver = counterType == null
                ? null
                : (Action<IExecutionObserver>) CreateDelegate(null,
                    counterType?.GetMethod(nameof(ExecutionObserverProxy.SetObserver),
                        new[] {typeof(IExecutionObserver)}));
        }

        public void InternalInitialize(ISmartContractBridgeContext context)
        {
            _methodInternalInitialize(context);
        }

        public TransactionExecutingStateSet GetChanges()
        {
            return _methodGetChanges();
        }

        internal void Cleanup()
        {
            _methodCleanup();
            _methodSetExecutionObserver?.Invoke(null);
            ResetFields();
        }

        void ResetFields()
        {
            _methodResetFields?.Invoke();
        }

        internal void SetExecutionObserver(IExecutionObserver observer)
        {
            _methodSetExecutionObserver?.Invoke(observer);
        }
    }
}