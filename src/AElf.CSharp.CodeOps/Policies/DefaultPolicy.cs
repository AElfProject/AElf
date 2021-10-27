using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.CodeOps.Patchers;
using AElf.CSharp.CodeOps.Validators;


namespace AElf.CSharp.CodeOps.Policies
{
    public interface IPolicy
    {
        List<IValidator<T>> GetValidators<T>();
        List<IPatcher<T>> GetPatchers<T>();
    }
    
    public class DefaultPolicy : IPolicy
    {
        public DefaultPolicy(IEnumerable<IValidator> validators, IEnumerable<IPatcher> patchers)
        {
            _validators = validators.ToList();
            _patchers = patchers.ToList();
        }

        private readonly List<IValidator> _validators;
        private readonly List<IPatcher> _patchers;

        public List<IValidator<T>> GetValidators<T>()
        {
            return _validators.OfType<IValidator<T>>().ToList();
        }

        public List<IPatcher<T>> GetPatchers<T>()
        {
            return _patchers.OfType<IPatcher<T>>().ToList();
        }
    }
}