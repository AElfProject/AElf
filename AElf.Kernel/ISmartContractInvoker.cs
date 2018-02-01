using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface ISmartContractInvoker
    {
        Task InvokeAsync(IAccountDataProvider accountDataProvider);
    }
}