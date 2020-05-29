using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.CodeOps.Validators;

namespace AElf.CSharp.CodeOps.Policies
{
    public static class SystemContractPolicyExtensions
    {
        public static List<IValidator<T>> FilterSystemContractApplicableValidators<T>(this IEnumerable<IValidator<T>> validators)
        {
            return validators.Where(v => !Constants.SystemContractInApplicableValidatorList.Contains(v.GetType()))
                .ToList();
        }
    }
}