using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.AssociationAuth;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class ContractAuditorTests : CSharpRuntimeTestBase
    {
        private ContractAuditor _auditor;

        public ContractAuditorTests()
        {
            _auditor = new ContractAuditor();
        }
        
        [Fact]
        public void CodeCheck_TestContract()
        {
            var code = ReadCode(typeof(TokenContract).Assembly.Location);

            Should.NotThrow(()=>_auditor.Audit(code, false));
        }

        [Fact]
        public void CodeCheck_SystemContracts()
        {
            var contracts = new[]
            {
                typeof(BasicContractZero),
                typeof(AssociationAuthContract),
                // typeof(ConsensusContract),
                // typeof(Contracts.Consensus.DPoS.SideChain.ConsensusContract),
                // typeof(CrossChainContract),
            };

            foreach (var contract in contracts)
            {
                var code = ReadCode(contract.Assembly.Location);
    
                Should.NotThrow(()=>_auditor.Audit(code, false));
            }
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }
}
