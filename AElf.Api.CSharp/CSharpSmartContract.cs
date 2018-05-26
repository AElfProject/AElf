using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Api.CSharp
{
    public abstract class CSharpSmartContract: ISmartContractWithContext
    {
        public void SetDataProvider(IDataProvider dataProvider)
        {
            Api.SetDataProvider(dataProvider);
        }

        public void SetContext(SmartContractRuntimeContext context)
        {
            Api.SetContext(context);
        }

        public abstract Task InitializeAsync(IAccountDataProvider dataProvider);
        public abstract Task<object> InvokeAsync(SmartContractInvokeContext context);
    }
}