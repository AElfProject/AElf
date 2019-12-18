using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IInValueCache
    {
        void AddInValue(long roundId, Hash inValue);
        Hash GetInValue(long roundId);
    }

    public class InValueCache : IInValueCache, ISingletonDependency
    {
        private readonly Dictionary<long, Hash> _inValues = new Dictionary<long, Hash>();

        public void AddInValue(long roundId, Hash inValue)
        {
            _inValues[roundId] = inValue;
        }

        public Hash GetInValue(long roundId)
        {
            // Remove old in values.
            foreach (var id in _inValues.Keys.Where(id => id < roundId))
            {
                _inValues.Remove(id);
            }

            _inValues.TryGetValue(roundId, out var inValue);
            return inValue ?? Hash.Empty;
        }
    }
}