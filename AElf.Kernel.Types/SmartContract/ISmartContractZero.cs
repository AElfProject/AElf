using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        byte[] DeploySmartContract(int category, byte[] code);
    }
}