using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task<Hash> DeploySmartContract(int category, byte[] contrac);
    }
}