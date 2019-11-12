using System.Collections.Generic;

namespace AElf.CSharp.CodeOps.Validators
{
    public interface IValidator<T>
    {
        IEnumerable<ValidationResult> Validate(T item);
    }
}