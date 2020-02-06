using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICodeCheckService
    {
        void Enable();
        void Disable();

        Task<bool> PerformCodeCheckAsync(byte[] code, Hash blockHash, long blockHeight);
    }
}