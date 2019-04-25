using System.Collections.Generic;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Validators.Type
{
    public class TypeValidator : IValidator<TypeReference>
    {
        public IEnumerable<ValidationResult> Validate(TypeReference item)
        {
            throw new System.NotImplementedException();
        }
    }
}