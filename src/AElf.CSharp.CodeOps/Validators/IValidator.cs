using System.Collections.Generic;
using System.Threading;

namespace AElf.CSharp.CodeOps.Validators
{
    public interface IValidator<T>
    {
        IEnumerable<ValidationResult> Validate(T item,  CancellationToken ct);
    }
}