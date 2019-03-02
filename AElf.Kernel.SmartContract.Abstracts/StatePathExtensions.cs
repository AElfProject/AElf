using System.Linq;

namespace AElf.Kernel.SmartContract
{
    public static class StatePathExtensions
    {
        public static string ToStateKey(this StatePath statePath)
        {
            return string.Join("/", statePath.Path.Select(x => x.ToStringUtf8()));
        }
    }
}