using System;
using System.Linq;
using AElf.Kernel.SmartContract;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AElf.CSharp.CodeOps;

// Injected into contract, not used directly, kept as a template to compare IL codes
public static class ExecutionObserverProxy
{
    [ThreadStatic] private static IExecutionObserver _observer;

    public static void SetObserver(IExecutionObserver observer)
    {
        _observer = observer;
    }

    public static void BranchCount()
    {
        if (_observer != null)
            _observer.BranchCount();
    }

    public static void CallCount()
    {
        if (_observer != null)
            _observer.CallCount();
    }
}

public class ExecutionObserverProxyChecker
{
    private readonly ModuleDefinition _module;

    private TypeDefinition _contractImplementationType;

    private TypeDefinition ContractImplementationType
    {
        get
        {
            if (_contractImplementationType == null)
            {
                bool BaseTypeIsInTheSameAssembly(TypeDefinition t)
                {
                    return t.BaseType is TypeDefinition;
                }

                _contractImplementationType = _module.GetAllTypes()
                    .Where(t => t.IsContractImplementation())
                    .First(BaseTypeIsInTheSameAssembly);
            }

            return _contractImplementationType;
        }
    }

    private string ObserverFieldName =>
        $"AElf.Kernel.SmartContract.IExecutionObserver {ContractImplementationType.Namespace}.{nameof(ExecutionObserverProxy)}::_observer";

    public ExecutionObserverProxyChecker(ModuleDefinition module)
    {
        _module = module;
    }

    public bool IsObserverFieldThatRequiresResetting(FieldDefinition field)
    {
        return field.FullName == ObserverFieldName;
    }
}