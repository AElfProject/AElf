using System.Collections.Generic;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IInValueCacheService
    {
        void AddInValue(long roundId, Hash inValue);
        Hash GetInValue(long roundId);
    }

    public class InValueCacheService : IInValueCacheService, ISingletonDependency
    {
        private readonly Dictionary<long, Hash> _inValues = new Dictionary<long, Hash>();

        public void AddInValue(long roundId, Hash inValue)
        {
            _inValues[roundId] = inValue;
        }

        public Hash GetInValue(long roundId)
        {
            _inValues.TryGetValue(roundId, out var inValue);
            return inValue ?? Hash.Empty;
        }
    }
}