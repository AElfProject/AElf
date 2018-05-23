using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    
    public interface ISmartContract 
    {
        Task InitializeAsync(IAccountDataProvider dataProvider);
        Task<object> InvokeAsync(SmartContractInvokeContext context);
    }
}