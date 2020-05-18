using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AElf.Kernel.CodeCheck.Infrastructure
{
    public interface IContractAuditorContainer
    {
        bool TryGetContractAuditor(int category, out IContractAuditor contractAuditor);
    }

    public class ContractAuditorContainer : IContractAuditorContainer
    {
        private readonly ConcurrentDictionary<int, IContractAuditor> _contractAuditors =
            new ConcurrentDictionary<int, IContractAuditor>();

        public ContractAuditorContainer(IEnumerable<IContractAuditor> contractAuditors)
        {
            foreach (var auditor in contractAuditors)
            {
                _contractAuditors[auditor.Category] = auditor;
            }
        }

        public bool TryGetContractAuditor(int category, out IContractAuditor contractAuditor)
        {
            return _contractAuditors.TryGetValue(category, out contractAuditor);
        }
    }
}