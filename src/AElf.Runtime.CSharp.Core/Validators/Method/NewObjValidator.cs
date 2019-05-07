using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class NewObjValidator : IValidator<MethodDefinition>
    {
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasBody)
                return Enumerable.Empty<ValidationResult>();

            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.OpCode.Code == Code.Newobj)
                {
                    if (instruction.Operand is MethodReference metRef)
                    {
                        if (!metRef.DeclaringType.IsValueType)
                        {
                            return new List<ValidationResult>
                            {
                                new NewObjValidationResult(method.FullName + "Creation of objects is not supported.")
                            };
                        }
                    }
                }
            }
            
            return Enumerable.Empty<ValidationResult>();
        }
    }
    
    public class NewObjValidationResult : ValidationResult
    {
        public NewObjValidationResult(string message) : base(message)
        {
        }
    }
}