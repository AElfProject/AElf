using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;

namespace AElf.Sdk.CSharp
{
    public class CSharpSmartContract: ISmartContract
    {
    }

    /*public partial class Authorization : CSharpSmartContract
    {
        public static readonly string AuthorizationVersion = "__AuthorizationVersion__";
        protected readonly UInt64Field AuthVersion = new UInt64Field(AuthorizationVersion);

        protected void UpdateVersion()
        {
            AuthVersion.SetValue(AuthVersion.GetValue() + 1);
        }
    }*/
}