using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class MultiDimArrayValidator : IValidator<MethodDefinition>
    {
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();
            
            var errors = new List<FloatValidationResult>();
            
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode.Code == Code.Newobj)
                {
                    var methodRef = (MethodReference) instruction.Operand;

                    if (methodRef.DeclaringType.IsArray && ((ArrayType) methodRef.DeclaringType).Dimensions.Count > 1)
                    {
                        return new List<ValidationResult>
                        {
                            new MultiDimArrayValidationResult(
                                method.Name + " contains multi dimension array declaration.")
                        };
                    }
                }
            }

            return errors;
        }
    }

    public class MultiDimArrayValidationResult : ValidationResult
    {
        public MultiDimArrayValidationResult(string message) : base(message)
        {
        }
    }
}