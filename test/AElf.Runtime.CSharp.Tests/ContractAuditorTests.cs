using System.IO;
using System.Linq;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
        public void CodeCheck_WithoutPrivilege()
        {
            var code = ReadCode(typeof(TestContract.TestContract).Assembly.Location);
            
            var result = _auditor.Audit(code, false);
            
            result.ShouldBeEmpty();
        }

        private byte[] ReadCode(string path)
        {
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
    }

}