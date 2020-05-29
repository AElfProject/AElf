using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.CodeOps.Validators;
using Volo.Abp.DependencyInjection;


namespace AElf.CSharp.CodeOps.Policies
{
    public interface IAuditPolicy
    {
        List<IValidator<T>> GetValidators<T>();
    }
    
    public class DefaultAuditPolicy : IAuditPolicy
    {
        public DefaultAuditPolicy(IEnumerable<IValidator> validators)
        {
            _validators = validators.ToList();
        }

        private readonly List<IValidator> _validators;

        public List<IValidator<T>> GetValidators<T>()
        {
            return _validators.OfType<IValidator<T>>().ToList();
        }
    }
}