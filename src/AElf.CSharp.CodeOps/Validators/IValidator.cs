using System.Collections.Generic;
using System.Threading;

namespace AElf.CSharp.CodeOps.Validators
{
    public interface IValidator
    {
    }

    public interface IValidator<T> : IValidator
    {
        IEnumerable<ValidationResult> Validate(T item, CancellationToken ct);
    }
}