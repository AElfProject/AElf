using System.Reflection;
using AElf.Kernel.SmartContract;
using AElf.Types;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblySmartContractProxy
{
    private readonly Action _methodCleanup;
    private readonly Func<TransactionExecutingStateSet> _methodGetChanges;
    private readonly Action<ISmartContractBridgeContext> _methodInternalInitialize;

    private readonly Action? _methodResetFields; // can be null

    public WebAssemblySmartContractProxy(object instance)
    {
        var instanceType = instance.GetType();

        _methodCleanup = CreateDelegate<Action>(instance, instanceType, nameof(Cleanup))!;

        _methodGetChanges =
            CreateDelegate<Func<TransactionExecutingStateSet>>(instance, instanceType, nameof(GetChanges))!;

        _methodInternalInitialize =
            CreateDelegate<Action<ISmartContractBridgeContext>>(instance, instanceType,
                nameof(InternalInitialize))!;

        _methodResetFields = CreateDelegate<Action>(instance, instanceType, nameof(ResetFields));
    }
    
    private static MethodInfo? GetMethodInfo(Type type, string name)
    {
        return type.GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
    }
    
    private static T CreateDelegate<T>(object instance, MethodInfo method)
        where T : Delegate
    {
        return (T)Delegate.CreateDelegate
        (
            typeof(T),
            instance,
            method
        );
    }

    private static T? CreateDelegate<T>(object instance, Type type, string name)
        where T : Delegate
    {
        var methodInfo = GetMethodInfo(type, name);

        return methodInfo == null ? null : CreateDelegate<T>(instance, methodInfo);
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
        ResetFields();
    }

    private void ResetFields()
    {
        _methodResetFields?.Invoke();
    }
}