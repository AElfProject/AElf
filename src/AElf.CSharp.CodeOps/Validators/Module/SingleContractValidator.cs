using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Module;

public class SingleContractValidator: IValidator<ModuleDefinition>, ITransientDependency
{
    public IEnumerable<ValidationResult> Validate(ModuleDefinition module, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();
        var contractImplementationCount = module.GetAllTypes()
            .Count(t => t.IsContractImplementation() && !t.IsNested);
        return contractImplementationCount switch
        {
            > 1 => new[]
            {
                new SingleContractValidationResult("Only one contract implementation is allowed in the assembly.")
            },
            0 => new[] {new SingleContractValidationResult("Contract implementation is not found in the assembly.")},
            _ => Array.Empty<ValidationResult>()
        };
    }

    public bool SystemContactIgnored => true;
}

public class SingleContractValidationResult : ValidationResult
{
    public SingleContractValidationResult(string message) : base(message)
    {
    }
}