using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module;

public class NoStaticFieldsValidator : IValidator<ModuleDefinition>, ITransientDependency
{
    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        var types = module.GetAllTypes().ToList();

        var checker = new ExecutionObserverProxyChecker(module);

        return types
            .SelectMany(t => t.Fields)
            .Where(f => f.IsDisallowedStaticField())
            .Where(f=>!checker.IsObserverFieldThatRequiresResetting(f)).Select(f =>
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