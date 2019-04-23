using System.Linq;
using AElf.Kernel;

namespace AElf
{
    public static class StateKeyExtensions
    {
        public static string ToStateKey(this StatePath statePath, Address address)
        {
            return new ScopedStatePath()
            {
                Address = address,
                Path = statePath
            }.ToStateKey();
        }

        public static string ToStateKey(this ScopedStatePath scopedStatePath)
        {
            return string.Join("/",
                new[] {scopedStatePath.Address.GetFormatted()}.Concat(scopedStatePath.Path.Parts));
        }
    }
}