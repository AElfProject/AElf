using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public interface IStateProvider
    {
        byte[] Get(StatePath path);
    }
}