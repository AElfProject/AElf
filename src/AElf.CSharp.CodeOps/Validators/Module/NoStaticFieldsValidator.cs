using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Protobuf.Reflection;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module;

public class NoStaticFieldsValidator : IValidator<ModuleDefinition>, ITransientDependency
{
    public static HashSet<string> FuncTypes { get; } = new ()
    {
        "System.Func`1", "System.Func`2", "System.Func`3", "System.Func`4", "System.Func`5", "System.Func`6",
        "System.Func`7", "System.Func`8", "System.Func`9", "System.Func`10", "System.Func`11", "System.Func`12",
        "System.Func`13", "System.Func`14", "System.Func`15", "System.Func`16", "System.Func`17"
    };

    private static bool IsFuncTypeFieldInGeneratedClass(FieldDefinition field)
    {
        if (field.DeclaringType.Name != "<>c")
        {
            return false;
        }
        if (field.FieldType is GenericInstanceType genericInstance)
        {
            return FuncTypes.Contains(genericInstance.ElementType.FullName);
        }

        return false;
    }

    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        bool BaseTypeIsInTheSameAssembly(TypeDefinition t)
        {
            return t.BaseType is TypeDefinition;
        }

        var types = module.GetAllTypes().ToList();

        var contractNamespace = types
            .Where(t => t.IsContractImplementation())
            .Single(BaseTypeIsInTheSameAssembly)
            .Namespace;

        var exceptionField = $"AElf.Kernel.SmartContract.IExecutionObserver {contractNamespace}.{nameof(ExecutionObserverProxy)}::_observer";

        var results = types
            .SelectMany(t => t.Fields)
            .Where(f => f.IsStatic && !f.IsInitOnly && !f.HasConstant)
            .Where(f => f.FieldType.FullName != typeof(FileDescriptor).FullName &&
                        f.FieldType.FullName != typeof(MessageDescriptor).FullName && !IsFuncTypeFieldInGeneratedClass(f)
            ).ToList();
        return results.Where(f => f.FullName != exceptionField).Select(f =>
                new HasStaticFieldsValidationResult("Has static field").WithInfo(
                    null,
                    f.DeclaringType.Namespace, f.DeclaringType.FullName, f.FullName)
            )
            .ToList();
    }

    public bool SystemContactIgnored => false;
}

public class HasStaticFieldsValidationResult : ValidationResult
{
    public HasStaticFieldsValidationResult(string message) : base(message)
    {
    }
}