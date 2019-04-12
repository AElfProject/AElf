using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Sdk
{
    public interface IStateProvider
    {
        IStateCache Cache { get; set; }
        Task<byte[]> GetAsync(StatePath path);
    }

    public interface IScopedStateProvider : IStateProvider
    {
        Address ContractAddress { get; }
    }
}