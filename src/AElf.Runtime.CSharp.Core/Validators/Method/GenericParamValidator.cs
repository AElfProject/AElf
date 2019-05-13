using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Validators.Method
{
    public class GenericParamValidator : IValidator<MethodDefinition>
    {
        public IEnumerable<ValidationResult> Validate(MethodDefinition method)
        {
            if (!method.HasGenericParameters)
                return Enumerable.Empty<GenericParamValidationResult>();

            return new List<ValidationResult>
            {
                new GenericParamValidationResult($"{method.FullName} contains generic parameter.")
            };
        }
    }
    
    public class GenericParamValidationResult : ValidationResult
    {
        public GenericParamValidationResult(string message) : base(message)
        {
        }
    }
}