using System.Collections.Generic;
using AElf.Contracts.CrossChain;

namespace AElf.CrossChain
{
    public class CrossChainContractPrivilegeMethodNameProvider
    {
        public static readonly List<string> PrivilegeMethodNames = new List<string>
        {
            nameof(CrossChainContractContainer.CrossChainContractStub.ProposeCrossChainIndexing),
            nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseCrossChainIndexing),
        };
    }
}