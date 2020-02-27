using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Validators.Method
{
    public class UncheckedMathValidator : IValidator<MethodDefinition>
    {
        private readonly HashSet<OpCode> _uncheckedOpCodes = new HashSet<OpCode>
        {
            OpCodes.Add,
            OpCodes.Sub,
            OpCodes.Mul
        };
        
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();

            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (!_uncheckedOpCodes.Contains(instruction.OpCode))
                    continue;
                
                errors.Add(
                    new UncheckedMathValidationResult( $"{method.Name} contains unsafe OpCode " + instruction.OpCode)
                            .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, null));
            }
            
            return errors;
        }
    }
    
    public class UncheckedMathValidationResult : ValidationResult
    {
        public UncheckedMathValidationResult(string message) : base(message)
        {
        }
    }
}