using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Sdk
{
    public interface IStateProvider
    {
        Task<byte[]> GetAsync(StatePath path);
    }

    public interface ICachedStateProvider : IStateProvider
    {
        IStateCache Cache { get; set; }
    }

    public interface IScopedStateProvider : ICachedStateProvider
    {
        Address ContractAddress { get; }
    }
}