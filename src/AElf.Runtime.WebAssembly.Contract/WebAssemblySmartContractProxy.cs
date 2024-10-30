using System.Reflection;
using AElf.Kernel.SmartContract;
using AElf.Runtime.WebAssembly.TransactionPayment;
using AElf.Types;

namespace AElf.Runtime.WebAssembly.Contract;

public class WebAssemblySmartContractProxy
{
    private readonly Action _methodCleanup;
    private readonly Func<TransactionExecutingStateSet> _methodGetChanges;
    private readonly Action<ISmartContractBridgeContext> _methodInternalInitialize;
    private readonly Func<IGasMeter> _methodGetGasMeter;

    private readonly Action? _methodResetFields; // can be null
    private readonly Func<List<string>>? _methodGetRuntimeLogs; // can be null
    private readonly Func<List<string>>? _methodGetCustomPrints; // can be null
    private readonly Func<List<string>>? _methodGetErrorMessages; // can be null
    private readonly Func<List<string>>? _methodGetDebugMessages; // can be null
    private readonly Func<List<(byte[], byte[])>>? _methodGetEvents; // can be null

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
        _methodGetGasMeter = CreateDelegate<Func<IGasMeter>>(instance, instanceType, nameof(GetGasMeter))!;
        _methodGetEvents = CreateDelegate<Func<List<(byte[], byte[])>>>(instance, instanceType, nameof(GetEvents));
        _methodGetRuntimeLogs = CreateDelegate<Func<List<string>>>(instance, instanceType, nameof(GetRuntimeLogs));
        _methodGetCustomPrints = CreateDelegate<Func<List<string>>>(instance, instanceType, nameof(GetCustomPrints));
        _methodGetErrorMessages = CreateDelegate<Func<List<string>>>(instance, instanceType, nameof(GetErrorMessages));
        _methodGetDebugMessages = CreateDelegate<Func<List<string>>>(instance, instanceType, nameof(GetDebugMessages));
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

    public IGasMeter GetGasMeter()
    {
        return _methodGetGasMeter.Invoke();
    }

    public List<string>? GetRuntimeLogs()
    {
        return _methodGetRuntimeLogs?.Invoke();
    }

    public List<string>? GetCustomPrints()
    {
        return _methodGetCustomPrints?.Invoke();
    }

    public List<string>? GetDebugMessages()
    {
        return _methodGetDebugMessages?.Invoke();
    }

    public List<string>? GetErrorMessages()
    {
        return _methodGetErrorMessages?.Invoke();
    }

    public List<(byte[], byte[])>? GetEvents()
    {
        return _methodGetEvents?.Invoke();
    }

    public void Cleanup()
    {
        _methodCleanup();
        ResetFields();
    }

    private void ResetFields()
    {
        _methodResetFields?.Invoke();
    }
}