using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.CSharp.CodeOps.Instructions;
using Mono.Cecil;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class InstructionInjectionValidator : IValidator<ModuleDefinition>, ITransientDependency
    {
        private readonly List<IInstructionInjector> _instructionInjectors;

        public InstructionInjectionValidator(IEnumerable<IInstructionInjector> instructionInjectors)
        {
            _instructionInjectors = instructionInjectors.ToList();
        }

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
            var result = new List<ValidationResult>();

            if (!methodDefinition.HasBody)
                return result;

            foreach (var instructionInjector in _instructionInjectors)
            {
                foreach (var instruction in methodDefinition.Body.Instructions.Where(instruction =>
                    instructionInjector.IdentifyInstruction(instruction)).ToList())
                {
                    if (instructionInjector.ValidateInstruction(moduleDefinition, instruction))
                        continue;
                    result.Add(new MethodCallInjectionValidationResult(
                        $"{instructionInjector.GetType()} validation failed."));
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
}