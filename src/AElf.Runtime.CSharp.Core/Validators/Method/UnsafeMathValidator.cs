using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class UnsafeMathValidator : IValidator<MethodDefinition>
    {
        private readonly HashSet<OpCode> unsafeOpCodes = new HashSet<OpCode>
        {
            OpCodes.Add,
            OpCodes.Sub,
            OpCodes.Mul,
            OpCodes.Div
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
                
                errors.Add(new UnsafeMathValidationResult(method.Name + " contains unsafe opcode " + instruction.OpCode));
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