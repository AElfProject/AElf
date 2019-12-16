using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICodeCheckService
    {
        void Enable();
        void Disable();

        Task<bool> PerformCodeCheckAsync(byte[] code);
    }
}