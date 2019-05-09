using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class FloatOpsValidator : IValidator<MethodDefinition>
    {
        private static readonly HashSet<OpCode> FloatOpCodes = new HashSet<OpCode>
        {
            OpCodes.Ldc_R4,
            OpCodes.Ldc_R8,
            OpCodes.Ldelem_R4,
            OpCodes.Ldelem_R8,
            OpCodes.Conv_R_Un,
            OpCodes.Conv_R4,
            OpCodes.Conv_R8,
            OpCodes.Ldind_R4,
            OpCodes.Ldind_R8,
            OpCodes.Stelem_R4,
            OpCodes.Stelem_R8,
            OpCodes.Stind_R4,
            OpCodes.Stind_R8
        };
        
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();
            var errors = new List<ValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (FloatOpCodes.Contains(instruction.OpCode))
                {
                    errors.Add(
                        new FloatOpsValidationResult("Method " + method.Name + " contains " + instruction.OpCode + " opcode.")
                                 .WithInfo(method.GetType().Namespace, method.GetType().Name, method.Name, null));
                }
            }

            return errors;
        }
    }
    
    public class FloatOpsValidationResult : ValidationResult
    {
        public FloatOpsValidationResult(string message) : base(message)
        {
        }
    }
}