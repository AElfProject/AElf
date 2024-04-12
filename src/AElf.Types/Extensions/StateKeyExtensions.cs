using System.Linq;
using AElf.Types;

namespace AElf
{

    public static class StateKeyExtensions
    {
        public static string ToStateKey(this StatePath statePath, Address address)
        {
            return new ScopedStatePath
            {
                Address = address,
                Path = statePath
            }.ToStateKey();
        }

        public static string ToStateKey(this ScopedStatePath scopedStatePath)
        {
            return string.Join("/",
                new[] { scopedStatePath.Address.ToBase58() }.Concat(scopedStatePath.Path.Parts));
        }
    }
}