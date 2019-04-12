using System.Diagnostics.CodeAnalysis;

namespace AElf.Types.CSharp
{
    public class ContractStubBase
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStubFactory __factory { get; set; }
    }
}