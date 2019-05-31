using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class UncheckedMathValidator : IValidator<MethodDefinition>
    {
        private readonly HashSet<OpCode> unsafeOpCodes = new HashSet<OpCode>
        {
            OpCodes.Add,
            OpCodes.Sub,
            OpCodes.Mul
        };
        
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<GenericParamValidationResult>();

            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (!unsafeOpCodes.Contains(instruction.OpCode))
                    continue;
                
                errors.Add(
                    new UnsafeMathValidationResult( $"{method.Name} contains unsafe OpCode " + instruction.OpCode)
                            .WithInfo(method.Name, method.DeclaringType.Namespace, method.DeclaringType.Name, null));
            }
            
            return errors;
        }
    }
    
    public class UnsafeMathValidationResult : ValidationResult
    {
        public UnsafeMathValidationResult(string message) : base(message)
        {
        }
    }
}