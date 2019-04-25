using System.Collections.Generic;

namespace AElf.Runtime.CSharp.Validators
{
    public interface IValidator<T>
    {
        IEnumerable<ValidationResult> Validate(T item);
    }
}