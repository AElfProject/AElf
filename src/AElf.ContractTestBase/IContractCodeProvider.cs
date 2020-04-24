using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    public interface IContractCodeProvider
    {
        IReadOnlyDictionary<string, byte[]> Codes { get; set; }
    }

    public class ContractCodeProvider : IContractCodeProvider, ISingletonDependency
    {
        public IReadOnlyDictionary<string, byte[]> Codes { get; set; }
    }
}