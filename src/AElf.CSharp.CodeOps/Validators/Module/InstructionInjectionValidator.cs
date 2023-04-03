using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.CSharp.CodeOps.Instructions;
using Mono.Cecil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method;

public class InstructionInjectionValidator : IValidator<ModuleDefinition>, ITransientDependency
{
    private readonly IStateWrittenInstructionInjector _instructionInjector;

    public InstructionInjectionValidator(IStateWrittenInstructionInjector instructionInjector)
    {
        _instructionInjector = instructionInjector;
    }

    public bool SystemContactIgnored => true;

    public IEnumerable<ValidationResult> Validate(ModuleDefinition moduleDefinition, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            throw new ContractAuditTimeoutException();

        var result = new List<ValidationResult>();
        foreach (var typ in moduleDefinition.Types)
        {
            result.AddRange(ValidateType(typ, moduleDefinition));
        }

        return result;
    }

    private List<ValidationResult> ValidateType(TypeDefinition type, ModuleDefinition moduleDefinition)
    {
        var result = new List<ValidationResult>();

        foreach (var method in type.Methods)
        {
            result.AddRange(ValidateMethod(moduleDefinition, method));
        }

        foreach (var nestedType in type.NestedTypes)
        {
            result.AddRange(ValidateType(nestedType, moduleDefinition));
        }

        return result;
    }

    private List<ValidationResult> ValidateMethod(ModuleDefinition moduleDefinition, MethodDefinition methodDefinition)
    {
        if (!methodDefinition.HasBody)
            return new List<ValidationResult>();
        var isNotContractImplementation = !methodDefinition.DeclaringType.IsContractImplementation();

        var result = new List<ValidationResult>();
        foreach (var instruction in methodDefinition.Body.Instructions.Where(instruction =>
                     _instructionInjector.IdentifyInstruction(instruction)).ToList())
        {
            // TODO: https://github.com/AElfProject/AElf/issues/3387
            if (isNotContractImplementation)
            {
                result.Add(new MethodCallInjectionValidationResult(
                    $"{_instructionInjector.GetType()} validation failed. Updating state in non-contract class is not allowed."
                ).WithInfo(
                    methodDefinition.Name,
                    methodDefinition.DeclaringType.Namespace,
                    methodDefinition.DeclaringType.FullName,
                    null
                ));
            }
            else if (!_instructionInjector.ValidateInstruction(moduleDefinition, instruction))
            {
                result.Add(new MethodCallInjectionValidationResult(
                    $"{_instructionInjector.GetType()} validation failed.").WithInfo(methodDefinition.Name,
                    methodDefinition.DeclaringType.Namespace, methodDefinition.DeclaringType.FullName, null));
            }
        }

        return result;
    }
}

public class MethodCallInjectionValidationResult : ValidationResult
{
    public MethodCallInjectionValidationResult(string message) : base(message)
    {
    }
}