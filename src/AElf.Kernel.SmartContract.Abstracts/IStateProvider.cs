using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public interface ICachedStateProvider : IStateProvider
    {
        IStateCache Cache { get; set; }
    }

    public interface IScopedStateProvider : ICachedStateProvider
    {
        Address ContractAddress { get; }
    }
}