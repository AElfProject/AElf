using System.Diagnostics.CodeAnalysis;

namespace AElf.Types.CSharp
{
    public class ContractTesterBase
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public ITestMethodFactory __factory { get; set; }
    }
}