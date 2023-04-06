using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Cecil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Assembly;

public class SingleModuleValidator: IValidator<AssemblyDefinition>, ITransientDependency
{
    public IEnumerable<ValidationResult> Validate(AssemblyDefinition assembly, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();
        if (assembly.Modules.Count > 1)
        {
            return new[]
            {
                new SingleModuleValidationResult("Only one module is allowed in the assembly.")
            };
        }
        return Array.Empty<ValidationResult>();
    }

    public bool SystemContactIgnored => true;
}

public class SingleModuleValidationResult : ValidationResult
{
    public SingleModuleValidationResult(string message) : base(message)
    {
    }
}