using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContract: ISmartContract
    {
        public abstract Task InvokeAsync();
    }
}