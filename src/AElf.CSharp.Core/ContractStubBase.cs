using System.Diagnostics.CodeAnalysis;

namespace AElf.CSharp.Core
{
    public class ContractStubBase
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStubFactory __factory { get; set; }
    }
}